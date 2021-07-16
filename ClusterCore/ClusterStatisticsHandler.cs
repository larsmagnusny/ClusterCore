using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class ClusterStatisticsHandler : WebSocketHandler
    {
        public ClusterStatisticsHandler(SocketManager socketManager) : base(socketManager)
        {

        }

        public override async Task ReceiveAsync(WebSocket socket, byte[] buffer)
        {
            string jsonString = Encoding.UTF8.GetString(buffer).Trim('\0');
            Guid id = WebSocketConnectionManager.GetId(socket);
            //Console.WriteLine(jsonString);
            var stats = JsonConvert.DeserializeObject<ClientStatistics>(jsonString);

            WebSocketConnectionManager.SetStatistics(id, stats);

            
            return;
        }
    }
}
