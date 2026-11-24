using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment the code below to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();
Socket client = server.AcceptSocket();

byte[] buffer = new byte[1024];
while (true)
{
    int bytesRead = client.Receive(buffer);
    if (bytesRead == 0)
    {
        Console.WriteLine("Connection closed");
        break;
    }
    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
    Console.WriteLine(request);
    client.Send(Encoding.UTF8.GetBytes("+PONG\r\n"));
}

