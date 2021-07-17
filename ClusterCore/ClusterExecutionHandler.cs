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
            return ConnectionManager.GetAll();
        }

        public void ResetSockets(IEnumerable<ClientSocket> sockets)
        {
            foreach (var item in sockets)
            {
                ConnectionManager.SetReady(item.Id, false);
            }
        }

        public override async Task ReceiveAsync(ClientSocket clientSocket, byte[] buffer)
        {
            // Recieve results from execution...
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            ConnectionManager.SetReady(clientSocket.Id, true);
            
            while (ConnectionManager.IsReady(clientSocket.Id))
            {
                Thread.Sleep(200);
            }
        }
    }
}
