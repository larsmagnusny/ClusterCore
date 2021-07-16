using ClusterCore.Utilities;
using System;
using System.Collections.Generic;
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

        public IEnumerable<ClientSocket> GetAllSockets()
        {
            return WebSocketConnectionManager.GetAll();
        }

        public void ResetSockets(IEnumerable<ClientSocket> sockets)
        {
            foreach (var item in sockets)
            {
                WebSocketConnectionManager.SetReady(item.Id, false);
            }
        }

        public override async Task ReceiveAsync(ClientSocket clientSocket, byte[] buffer)
        {
            // Recieve results from execution...
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            WebSocketConnectionManager.SetReady(clientSocket.Id, true);
            
            while (WebSocketConnectionManager.IsReady(clientSocket.Id))
            {
                Thread.Sleep(200);
            }
        }
    }
}
