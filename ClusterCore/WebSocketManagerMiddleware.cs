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

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);

            await Receive(socket, async (buffer) =>
            {
                try
                {
                    await _webSocketHandler.ReceiveAsync(socket, buffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        private async Task Receive(WebSocket socket, Action<byte[]> handleMessage)
        {
            while (socket.State == WebSocketState.Open)
            {
                byte[] buffer = await SocketUtilities.ReadSocketUntillEnd(socket);

                handleMessage(buffer);
            }
        }
    }
}