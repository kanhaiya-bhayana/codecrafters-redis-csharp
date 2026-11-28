namespace codecrafters_redis.DTOs;

public record RedisList(
    List<string> Values
) : IRedisValue
{
    public List<String> PopLeft(int count)
    {
        count = Math.Min(Values.Count, count);
        var popped = Values.GetRange(0, count);
        Values.RemoveRange(0, count);
        return popped;
    }

    public string? PopLeft()
    {
        if (Values.Count == 0)
        {
            return null;
        }
        
        var value = Values[0];
        Values.RemoveAt(0);
        return value;
    }
}