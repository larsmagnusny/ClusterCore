using ClusterCore.Utilities;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore
{
    public  class WebSocketManagerMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketManagerMiddleware(RequestDelegate next,
                                            WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;

            var clientSocket = new ClientSocket { 
                Id = Guid.NewGuid(),
                Socket = await context.WebSockets.AcceptWebSocketAsync() 
            };

            await _webSocketHandler.OnConnected(clientSocket);

            await Receive(clientSocket, async (buffer) =>
            {
                try
                {
                    await _webSocketHandler.ReceiveAsync(clientSocket, buffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        private async Task Receive(ClientSocket clientSocket, Action<byte[]> handleMessage)
        {
            while (clientSocket.Socket.State == WebSocketState.Open)
            {
                byte[] buffer = await SocketUtilities.ReadSocketUntillEnd(clientSocket.Socket);

                handleMessage(buffer);
            }
        }
    }
}