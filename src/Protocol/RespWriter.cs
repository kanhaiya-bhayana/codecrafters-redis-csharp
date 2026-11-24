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

}