using ClusterCore.Utilities;
using System.Threading.Tasks;

namespace ClusterCore
{
    public abstract class WebSocketHandler
    {
        public SocketManager ConnectionManager { get; set; }

        public WebSocketHandler(SocketManager webSocketConnectionManager)
        {
            ConnectionManager = webSocketConnectionManager;
        }

        public virtual async Task OnConnected(ClientSocket socket)
        {
            ConnectionManager.AddSocket(socket);
        }

        public virtual async Task OnDisconnected(ClientSocket socket)
        {
            await ConnectionManager.RemoveSocket(socket);
        }


        public abstract Task ReceiveAsync(ClientSocket clientSocket, byte[] buffer);
    }
}
