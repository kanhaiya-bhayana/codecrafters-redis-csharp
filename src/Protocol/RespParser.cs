using codecrafters_redis.DTOs;

namespace codecrafters_redis.Utils;

public static class RespParser
{
    public static RedisCommand Parse(string request)
    {
        var parts = request.Split(
            ["\r\n"], 
            StringSplitOptions.RemoveEmptyEntries);

        string name = parts[2];

        var arguments = parts
            .Skip(4)
            .Where((_, index) => index % 2 == 0)
            .ToArray();

        return new RedisCommand(name, arguments);
    }
}