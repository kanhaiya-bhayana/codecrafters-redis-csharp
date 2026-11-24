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
                
                string key = command.Arguments[0];
                string value = command.Arguments[1];

                if (_store.TryGetValue(key, out var redisValue))
                {
                    if (redisValue is RedisList list)
                    {
                        list.Values.Add(value);
                        return RespWriter.Integer(
                            list.Values.Count);
                    }
                }

                var newList = new RedisList(
                    [value]);
                
                _store.TryAdd(key, newList);

                return RespWriter.Integer(1);
                
            default:
                return RespWriter.NullBulkString();
        }
    }
}