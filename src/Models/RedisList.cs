namespace codecrafters_redis.DTOs;

public record RedisList(
    List<string> Values
    ) : IRedisValue;