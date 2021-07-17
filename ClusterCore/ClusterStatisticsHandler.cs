using ClusterCore.Utilities;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class ClusterStatisticsHandler : WebSocketHandler
    {
        private static ClusterStatisticsHandler Instance;

        public ClusterStatisticsHandler(SocketManager socketManager) : base(socketManager)
        {
            Instance = this;
        }

        public override async Task ReceiveAsync(ClientSocket clientSocket, byte[] buffer)
        {
            string jsonString = Encoding.UTF8.GetString(buffer).Trim('\0');
            //Console.WriteLine(jsonString);
            var stats = JsonConvert.DeserializeObject<Metrics>(jsonString);

            ConnectionManager.SetStatistics(clientSocket.Id, stats);

            
            return;
        }

        public static ClusterStatisticsHandler GetInstance()
        {
            return Instance;
        }
    }
}
