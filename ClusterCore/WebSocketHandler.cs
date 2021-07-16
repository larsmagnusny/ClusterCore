using ClusterCore.Utilities;
using System.Threading.Tasks;

namespace ClusterCore
{
    public abstract class WebSocketHandler
    {
        protected SocketManager WebSocketConnectionManager { get; set; }

        public WebSocketHandler(SocketManager webSocketConnectionManager)
        {
            WebSocketConnectionManager = webSocketConnectionManager;
        }

        public virtual async Task OnConnected(ClientSocket socket)
        {
            WebSocketConnectionManager.AddSocket(socket);
        }

        public virtual async Task OnDisconnected(ClientSocket socket)
        {
            await WebSocketConnectionManager.RemoveSocket(socket);
        }


        public abstract Task ReceiveAsync(ClientSocket clientSocket, byte[] buffer);
    }
}
