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
            Console.WriteLine($" Curr Thread: {Task.CurrentId}");
            client.Send(Encoding.UTF8.GetBytes("+PONG\r\n"));
        }
    }
    finally{
        client.Close();
    }
}
