using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ClusterCore
{
    public static class Server
    {
        private static Thread SocketManagerThread = new Thread(ManageSockets);
        private static bool ManagerThreadRunning = false;
        public static void StartManagerThread()
        {
            ManagerThreadRunning = true;
            SocketManagerThread.Start();
        } 

        public static void StopManagerThread()
        {
            ManagerThreadRunning = false;
        }

        private static object clientLock = new object();
        private static Dictionary<string, WebSocket> IpClient = new Dictionary<string, WebSocket>();

        public static Func<HttpContext, Func<Task>, Task> HandleRequest = async (context, next) =>
        {
            try
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        if (AddClient(context.Connection.RemoteIpAddress.ToString(), webSocket))
                        {
                            string source = @"using System;
using System.Net;
              
namespace ClusterProgram
{
    class Program {
        static void Main(string[] args){
            for(int i = 0; i < 100; i++){
                Console.WriteLine(i.ToString() + "" is "" + (i % 2 == 0 ? ""Even"" : ""Odd""));
            }
        }
    }
}
                            ";

                            await SendProgram(context, webSocket, source);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            } 
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        };

        private static bool AddClient(string remoteIp, WebSocket socket)
        {
            lock (clientLock)
            {
                if (IpClient.ContainsKey(remoteIp))
                    return false;

                IpClient.Add(remoteIp, socket);

                return true;
            }
        }

        public static void ManageSockets()
        {
            List<WebSocket> socketsToClose = new List<WebSocket>();
            List<string> ipsClosed = new List<string>();
            while (ManagerThreadRunning)
            {
                lock (clientLock)
                {
                    foreach(var item in IpClient)
                    {
                        if (item.Value.State != WebSocketState.Open)
                        {
                            ipsClosed.Add(item.Key);
                            socketsToClose.Add(item.Value);
                            item.Value.Dispose();
                        }
                    }

                    foreach (var ip in ipsClosed)
                    {
                        Console.WriteLine(string.Concat(ip, " closed connection."));
                        IpClient.Remove(ip);
                    }
                }

                Thread.Sleep(500);
                socketsToClose.Clear();
                ipsClosed.Clear();
            }
        }

        private static async Task SendProgram(HttpContext context, WebSocket webSocket, string source)
        {
            var buffer = new byte[1024 * 4];
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
            WebSocketReceiveResult result = null;

            while (result == null || !result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(sourceBytes, 0, sourceBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));
            }
        }
    }
}
