using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class ClusterExecutionHandler : WebSocketHandler
    {
        public ClusterExecutionHandler(SocketManager socketManager) : base(socketManager)
        {

        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            // Recieve results from execution...
            return ;
        }
    }
}
