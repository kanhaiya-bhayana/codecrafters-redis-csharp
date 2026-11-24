namespace codecrafters_redis.DTOs;

public record RedisString(
    string Value,
    DateTimeOffset? ExpiredAt) : IRedisValue;
    
    