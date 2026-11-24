using System.Net;
using System.Net.Sockets;
using codecrafters_redis.Commands;
using codecrafters_redis.Networking;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

var commandHandler = new CommandHandler();
while (true)
{
    var client = server.AcceptSocket();
    _ = Task.Run(() =>
        ClientHandler.Handle(client, commandHandler));
}