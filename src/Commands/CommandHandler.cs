using System.Collections.Concurrent;
using codecrafters_redis.DTOs;
using codecrafters_redis.Utils;

namespace codecrafters_redis.Commands;

public class CommandHandler
{
    private readonly ConcurrentDictionary<string, IRedisValue> _store = new();

    public string Execute(string request)
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
                RedisList lst = GetOrCreateList(key);
                lst.Values.AddRange(values);
                return RespWriter.Integer(
                    lst.Values.Count);
            }

            case "LPUSH":
            {
                var (key, values) = GetKeyAndValues(command);
                RedisList list = GetOrCreateList(key);
                foreach (var s in values)
                    list.Values.Insert(0,s);
                return RespWriter.Integer(list.Values.Count);
            }

            case "LRANGE":
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