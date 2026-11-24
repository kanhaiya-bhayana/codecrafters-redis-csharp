using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Utils;

namespace codecrafters_redis.Networking;

public static class ClientHandler
{
    public static void Handle(
        Socket client,
        CommandHandler commandHandler)
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = client.Receive(buffer);
                if (bytesRead == 0)
                {
                    // Console.WriteLine($"Connection closed for Thread: {Task.CurrentId}");
                    break;
                }
                // Console.WriteLine($" Curr Thread: {Task.CurrentId}");
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                string response = commandHandler.Execute(request);
                
                client.Send(Encoding.UTF8.GetBytes(response));
            }
        }
        finally{
            client.Close();
        }
    }
}