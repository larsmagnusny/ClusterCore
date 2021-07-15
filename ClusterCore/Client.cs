using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore
{
    public static class Client
    {
        public static async void Run(object data)
        {
            string[] args = data as string[];
            Console.WriteLine("Waiting for server to be up...");
            Thread.Sleep(5000);

            var Uri = new Uri("ws://localhost:5000/ws");

            if(args.Length > 0)
            {
                Uri = new Uri(string.Concat(args[0], "/ws"));
            }
            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(120);
            await socket.ConnectAsync(Uri, CancellationToken.None);
            var buffer = Encoding.UTF8.GetBytes("Hello from client!");
            while (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
                await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
                Thread.Sleep(1000);
            }
        }
    }
}
