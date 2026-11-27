using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.DTOs;
using codecrafters_redis.Utils;

namespace codecrafters_redis.Commands;

public class CommandHandler
{
    private readonly ConcurrentDictionary<string, IRedisValue> _store = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TaskCompletionSource<(string Key, string Value)>>> _waitingQueue = new();

    public async Task<string> ExecuteAsync(string request)
    {
        RedisCommand command = RespParser.Parse(request);
    
        switch (command.Name.ToUpper())
        {
            case "PING":
                return RespWriter.SimpleString("PONG");
        
            case "ECHO":
                string message = command.Arguments[0];
                return RespWriter.BulkString(message);
        
            case "SET":
                DateTimeOffset? expireAt = null;

                if (command.Arguments.Length >= 3)
                {
                    long expiryValue = long.Parse(command.Arguments[3]);

                    expireAt = command.Arguments[2] switch
                    {
                        "PX" => DateTimeOffset.UtcNow.AddMilliseconds(expiryValue),
                        "EX" => DateTimeOffset.UtcNow.AddSeconds(expiryValue),
                        _ => null
                    };
                }

                _store[command.Arguments[0]] = new RedisString(
                    Value: command.Arguments[1],
                    ExpiredAt: expireAt
                );

                return RespWriter.Ok();
        
            case "GET":
                if (!_store.TryGetValue(command.Arguments[0], out var entry) || entry is not RedisString redisString)
                    return RespWriter.NullBulkString();

                if (redisString.ExpiredAt is null ||
                    !(DateTimeOffset.UtcNow >= redisString.ExpiredAt)) return RespWriter.BulkString(redisString.Value);
                _store.TryRemove(command.Arguments[0], out _);
                return RespWriter.NullBulkString();

            case "RPUSH":
            {
                var (key, values) = GetKeyAndValues(command);

                if (_waitingQueue.TryGetValue(key, out var waiters))
                {
                    int i = 0;

                    while (i < values.Length &&
                           waiters.TryDequeue(out var waiter))
                    {
                        waiter.SetResult((key, values[i]));
                        Console.WriteLine($"Waking waiter: {key} -> {values[i]}");
                        i++;
                    }

                    values = values.Skip(i).ToArray();
                }

                if (values.Length == 0)
                {
                    return RespWriter.Integer(0);
                }

                RedisList list = GetOrCreateList(key);

                list.Values.AddRange(values);

                return RespWriter.Integer(list.Values.Count);
            }

            case "LPUSH":
            {
                var (key, values) = GetKeyAndValues(command);

                if (_waitingQueue.TryGetValue(key, out var waiters))
                {
                    int remaining = values.Length;
                    Console.WriteLine($"Queue count before loop: {waiters.Count}");

                    while (remaining > 0 &&
                           waiters.TryDequeue(out var waiter))
                    {
                        remaining--;

                        Console.WriteLine(
                            $"Dequeued waiter. Remaining={remaining}. QueueCount={waiters.Count}");
                        
                        try
                        {
                            Console.WriteLine($"Before SetResult");

                            waiter.SetResult(
                                (key, values[remaining]));

                            Console.WriteLine($"After SetResult");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }

                        Console.WriteLine(
                            $"SetResult completed for {values[remaining]}");
                    }

                    values = values.Take(remaining).ToArray();
                }

                if (values.Length == 0)
                {
                    return RespWriter.Integer(0);
                }

                RedisList list = GetOrCreateList(key);

                foreach (var value in values)
                {
                    list.Values.Insert(0, value);
                }

                return RespWriter.Integer(list.Values.Count);
            }

            case "LRANGE":
            {
                string listKey = command.Arguments[0];
                int start = int.Parse(command.Arguments[1]);
                int end = int.Parse(command.Arguments[2]);

                if (!_store.TryGetValue(listKey, out var value))
                {
                    return RespWriter.Array([]);
                }

                if (value is not RedisList redisList)
                {
                    return RespWriter.Array([]);
                }
                
                if (start < 0) start += redisList.Values.Count;
                if (end < 0) end += redisList.Values.Count;
                
                int count = end - start + 1;

                var result = redisList.Values
                    .Skip(start)
                    .Take(count)
                    .ToList();
                
                return RespWriter.Array(result);
            }
            case "LLEN":
            {
                if (!_store.TryGetValue(command.Arguments[0], out var redisValue))
                {
                    return RespWriter.Integer(0);
                }

                if (redisValue is RedisList list)
                {
                    return RespWriter.Integer(list.Values.Count);
                }
                return RespWriter.Integer(0);
            }

            case "LPOP":
            {
                var (key, values) = GetKeyAndValues(command);

                if (!_store.TryGetValue(key, out var redisValue))
                {
                    return RespWriter.NullBulkString();
                }

                if (redisValue is not RedisList list) throw new InvalidOperationException("WRONGTYPE");
                
                var count = Math.Min(
                    values.Length > 0 ? int.Parse(values[0]) : 1,
                    list.Values.Count);

                var popped = list.Values.GetRange(0, count);

                list.Values.RemoveRange(0, count);

                return count == 1
                    ? RespWriter.BulkString(popped[0])
                    : RespWriter.Array(popped);
            }

            case "BLPOP":
            {
                var key = command.Arguments[0];
                
                Console.WriteLine(
                    $"BLPOP received for key={command.Arguments[0]}");
                
                // immediate pop if list already has values
                if (_store.TryGetValue(key, out var redisValue))
                {
                    if (redisValue is not RedisList list)
                    {
                        throw new InvalidOperationException("WRONGTYPE");
                    }

                    if (list.Values.Count > 0)
                    {
                        var val = list.Values[0];
                        list.Values.RemoveAt(0);
                        
                        return RespWriter.Array([key, val]);
                    }
                }
                
                // no value available block
                var tcs = new TaskCompletionSource<(string Key, string Value)>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                var queue = _waitingQueue.GetOrAdd(
                    key,
                    _ => new ConcurrentQueue<TaskCompletionSource<(string Key, string Value)>>());
                
                queue.Enqueue(tcs);

                Console.WriteLine($"Waiting on key: {key}");
                Console.WriteLine($"BLPOP waiter added. Queue count={queue.Count}");

                var result = await tcs.Task;
                
                return RespWriter.Array([result.Key, result.Value]);

            }
                
            default:
                return RespWriter.NullBulkString();
        }
    }

    private RedisList GetOrCreateList(string listKey)
    {
        if (_store.TryGetValue(listKey, out var redisValue))
        {
            if (redisValue is RedisList list)
            {
                return list;
            }

            throw new InvalidOperationException("WRONGTYPE");
        }

        var listValue = new RedisList([]);
        _store.TryAdd(listKey, listValue);
        return listValue;
    }

    private static (string Key, string[] Values)
        GetKeyAndValues(RedisCommand command)
    {
        return (command.Arguments[0], command.Arguments.Skip(1).ToArray());
    }
}