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

        private static string source = @"using System;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
              
namespace ClusterProgram
{
    public class ClientResult {
        public int Number { get; set; }
        public bool IsOdd { get;set; }
    }

    class Program {
        public static async Task<string> Evaluate(WebSocket socket, object parameters, string source)
        {
            byte[] buffer = new byte[4 * 1024];
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(source)), WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.ReceiveAsync(buffer, CancellationToken.None);

            return Encoding.UTF8.GetString(buffer);
        }

        static async Task Main(Dictionary<string, WebSocket> Clients, string source){
            // Process the results from clients
            var results = await Task.WhenAll(Clients.Select(o => Evaluate(o.Value, null, source)));
            
            Console.WriteLine(""Got: {0} results."", results.Length);
        }

        static List<ClientResult> Client(){
            var ret = new List<ClientResult>();
            for(int i = 0; i < 1000; i++){
                ret.Add(new ClientResult { Number = i, IsOdd = i % 2 != 0 });
            }

            return ret;
        }
    }
}
                            ";

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
                            

                            await SocketLoop(context, webSocket, source);
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

        private static async Task SocketLoop(HttpContext context, WebSocket webSocket, string source)
        {
            var buffer = new byte[1024 * 4];
            byte[] sourceBytes = Encoding.UTF8.GetBytes(source);
            WebSocketReceiveResult result = null;

            var program = new ClusterProgram(source);

            var entryPoint = program.GetServerEntryPoint();

            if (entryPoint == null)
            {
                Console.WriteLine("Program has no entry point for Server");
                return;
            }

            while (!webSocket.CloseStatus.HasValue)
            {
                if (entryPoint.GetParameters().Length > 0)
                {
                    Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { IpClient, source });
                    await invokeResult;
                }
                else
                    entryPoint.Invoke(null, null);

                Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));
                Thread.Sleep(100);
            }
        }
    }
}
