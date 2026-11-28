using System.Text;

namespace codecrafters_redis.Utils;

public static class RespWriter
{
    public static string Ok() =>
        "+OK\r\n";
    
    public static string SimpleString(string value) =>
        $"+{value}\r\n";

    public static string BulkString(string value) =>
        $"${value.Length}\r\n{value}\r\n";

    public static string NullBulkString() =>
        "$-1\r\n";

    public static string Integer(int? value) =>
        $":{value}\r\n";
    
    public static string NullArray() =>
        "*-1\r\n";

    public static string Array(IEnumerable<string> values)
    {
        var items = values.ToList();
        
        StringBuilder sb = new();
        sb.Append($"*{items.Count}\r\n");

        foreach (var item in items)
        {
            sb.Append(BulkString(item));
        }
        
        return sb.ToString();
    }

}