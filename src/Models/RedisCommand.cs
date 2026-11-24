namespace codecrafters_redis.DTOs;

public record RedisCommand(
    string Name,
    string[] Arguments);