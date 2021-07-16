using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class ClusterExecutionHandler : WebSocketHandler
    {
        public ClusterExecutionHandler(SocketManager socketManager) : base(socketManager)
        {

        }

        public WebSocket[] GetAllSockets()
        {
            return WebSocketConnectionManager.GetAll().Values.ToArray();
        }

        public void ResetSockets(WebSocket[] sockets)
        {
            foreach (var item in sockets)
            {
                Guid id = WebSocketConnectionManager.GetId(item);
                WebSocketConnectionManager.SetReady(id, false);
            }
        }

        public override async Task ReceiveAsync(WebSocket socket, byte[] buffer)
        {
            // Recieve results from execution...
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            Guid id = WebSocketConnectionManager.GetId(socket);
            WebSocketConnectionManager.SetReady(id, true);
            
            while (WebSocketConnectionManager.IsReady(id))
            {
                Thread.Sleep(200);
            }
        }
    }
}
