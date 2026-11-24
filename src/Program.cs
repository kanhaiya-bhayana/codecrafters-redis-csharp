using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
    Socket client = server.AcceptSocket(); // connects the clients
    _ = Task.Run(() =>
    {
        // handle clients
        HandleClient(client);
        
    });
    
}

static void HandleClient(Socket client)
{
    try
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            int bytesRead = client.Receive(buffer);
            if (bytesRead == 0)
            {
                Console.WriteLine("Connection closed");
                break;
            }
            // Console.WriteLine($" Curr Thread: {Task.CurrentId}");
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
 * *2\r\n$4\r\nECHO\r\n$3\r\nhey\r\n
 * *2   0
   $4   1
   ECHO 2
   $3   3
   hey  4
 */
static string ParseInput(string request)
{
    string[] parts = request.Split(
        ["\r\n"], StringSplitOptions.RemoveEmptyEntries);
    
    if (parts.Length >= 5 && parts[2] == "ECHO")
    {
        string message = parts[4];
        return $"${message.Length}\r\n{message}\r\n";
    }

    return $"+OK\r\n";

}