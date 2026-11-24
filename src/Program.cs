using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.DTOs;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

ConcurrentDictionary<string, RedisValue> store = new();
while (true)
{
    Socket client = server.AcceptSocket(); // connects the clients
    _ = Task.Run(() =>
    {
        // handle clients
        HandleClient(client);
        
    });
    
}

void HandleClient(Socket client)
{
    try
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            int bytesRead = client.Receive(buffer);
            if (bytesRead == 0)
            {
                Console.WriteLine($"Connection closed for Thread: {Task.CurrentId}");
                break;
            }
            Console.WriteLine($" Curr Thread: {Task.CurrentId}");
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string response = ParseInput(request);
            client.Send(Encoding.UTF8.GetBytes(response));
        }
    }
    finally{
        client.Close();
    }
}

/* Bulk string fromat: $<length>\r\n<data>\r\n
 * 
 * Input will be RESP Array
 * *<number-of-elements>\r\n<element-1>...<element-n>
 * *3\r\n$3\r\nSETr\n$3\r\nfoo\r\$3\r\nbar\r\n
 * *2   0
   $4   1
   ECHO 2
   $3   3
   hey  4
 */
string ParseInput(string request)
{
    string[] parts = request.Split(
        ["\r\n"], StringSplitOptions.RemoveEmptyEntries);
    
    string command = parts[2].ToUpper();
    
    switch (command)
    {
        case "PING":
            return $"+PONG\r\n";
        
        case "ECHO":
            string message = parts[4];
            return WrapResponse(message);
        
        case "SET":
            DateTimeOffset? expireAt = null;

            if (parts.Length > 8)
            {
                long expiryValue = long.Parse(parts[10]);

                expireAt = parts[8] switch
                {
                    "PX" => DateTimeOffset.UtcNow.AddMilliseconds(expiryValue),
                    "EX" => DateTimeOffset.UtcNow.AddSeconds(expiryValue),
                    _ => null
                };
            }

            store[parts[4]] = new RedisValue(
                Value: parts[6],
                ExpiredAt: expireAt
            );

            return ReturnOk();
        
        case "GET":
            if (!store.TryGetValue(parts[4], out var entry))
                return ReturnNull();

            if (entry.ExpiredAt is null ||
                !(DateTimeOffset.UtcNow >= entry.ExpiredAt)) return WrapResponse(entry.Value);
            store.TryRemove(parts[4], out _);
            return ReturnNull();

        default:
            return ReturnNull();
    }
}

string WrapResponse(string? input) => $"${input?.Length}\r\n{input}\r\n"; 
string ReturnOk() => "+OK\r\n";
string ReturnNull() => "$-1\r\n";