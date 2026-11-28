using System.Collections.Concurrent;
using codecrafters_redis.DTOs;
using codecrafters_redis.Utils;

namespace codecrafters_redis.Commands;

public class CommandHandler
{
    private readonly ConcurrentDictionary<string, IRedisValue> _store = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<WaitingClient>> _waitingQueue = new();

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

                int initialLength = 0;

                if (_store.TryGetValue(key, out var existingValue) &&
                    existingValue is RedisList existingList)
                {
                    initialLength = existingList.Values.Count;
                }

                int finalLength = initialLength + values.Length;

                if (_waitingQueue.TryGetValue(key, out var waiters))
                {
                    int index = 0;

                    while (index < values.Length &&
                           waiters.TryDequeue(out var waiter))
                    {
                        if (waiter.Tcs.Task.IsCompleted)
                        {
                            Console.WriteLine($"Waiter task already completed. Skipping.");
                            continue;
                        }
                        if (!waiter.Tcs.TrySetResult((key, values[index])))
                        {
                            continue;
                        }
                        // waiter.Tcs.SetResult(
                        //     (key, values[index]));

                        index++;
                    }

                    values = values.Skip(index).ToArray();
                }

                // fill the remaining list
                if (values.Length > 0)
                {
                    RedisList list = GetOrCreateList(key);
                    list.Values.AddRange(values);
                }

                return RespWriter.Integer(finalLength);
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

                        Console.WriteLine(
                            $"Dequeued waiter. Remaining={remaining}. QueueCount={waiters.Count}");
                        
                        try
                        {
                            Console.WriteLine($"Before SetResult");
                            if (waiter.Tcs.Task.IsCompleted)
                            {
                                Console.WriteLine($"Waiter task already completed. Skipping.");
                                continue;
                            }
                            remaining--;
                            if (!waiter.Tcs.TrySetResult((key, values[remaining])))
                            {
                                continue;
                            }
                            // waiter.Tcs.SetResult(
                            //     (key, values[remaining]));

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

                var popped = list.PopLeft(count);
                
                return count == 1
                    ? RespWriter.BulkString(popped[0])
                    : RespWriter.Array(popped);
            }

            case "BLPOP":
            {
                var (key, values) = GetKeyAndValues(command);
                double timeoutSeconds = values.Length > 0
                    ? double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture)
                    : 0;

                Console.WriteLine(
                    $"BLPOP received for key={command.Arguments[0]}");

                // no value available block
                var waitingClient = new WaitingClient(
                    new TaskCompletionSource<(string Key, string Value)>(
                        TaskCreationOptions.RunContinuationsAsynchronously));

                var queue = _waitingQueue.GetOrAdd(
                    key,
                    _ => new ConcurrentQueue<WaitingClient>());

                queue.Enqueue(waitingClient);

                Console.WriteLine($"Waiting on key: {key}");
                Console.WriteLine($"BLPOP waiter added. Queue count={queue.Count}");

                // Close the race between the initial empty check and enqueueing.
                if (_store.TryGetValue(key, out var redisValue) &&
                    redisValue is RedisList list && list.Values.Count > 0)
                {
                    var immediateValue = list.PopLeft();
                    if (immediateValue is not null &&
                        waitingClient.Tcs.TrySetResult((key, immediateValue)))
                    {
                        RemoveWaiter(key, waitingClient);
                        var result = await waitingClient.Tcs.Task;
                        return RespWriter.Array([result.Key, result.Value]);
                    }
                }

                var timeOutTask = timeoutSeconds == 0
                    ? Task.Delay(Timeout.InfiniteTimeSpan)
                    : Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

                var completedTask = await Task.WhenAny(waitingClient.Tcs.Task, timeOutTask);

                if (completedTask == waitingClient.Tcs.Task)
                {
                    var result = await waitingClient.Tcs.Task;
                    RemoveWaiter(key, waitingClient);
                    return RespWriter.Array([result.Key, result.Value]);
                }

                RemoveWaiter(key, waitingClient);
                waitingClient.Tcs.TrySetCanceled();
                return RespWriter.NullArray();
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

    private void RemoveWaiter(string key, WaitingClient waiter)
    {
        if (_waitingQueue.TryGetValue(key, out var queue))
        {
            var newQueue = new ConcurrentQueue<WaitingClient>(queue.Where(w => w != waiter));
            _waitingQueue[key] = newQueue;
        }
    }
}