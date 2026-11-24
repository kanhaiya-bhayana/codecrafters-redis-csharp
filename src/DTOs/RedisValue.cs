namespace codecrafters_redis.DTOs;

public record RedisValue(
    string Value, 
    DateTimeOffset? ExpiredAt);