using ClusterCore.Utilities;
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
    public class SocketManager
    {
        private ConcurrentDictionary<Guid, ClientSocket> _sockets = new ConcurrentDictionary<Guid, ClientSocket>();
        private ConcurrentDictionary<Guid, ClientStatistics> _statistics = new ConcurrentDictionary<Guid, ClientStatistics>();
        private ConcurrentDictionary<Guid, bool> _socketReady = new ConcurrentDictionary<Guid, bool>();

        public ClientSocket GetSocketById(Guid id)
        {
            return _sockets[id];
        }

        public void SetReady(Guid id, bool ready)
        {
            _socketReady[id] = ready;
        }

        public bool IsReady(Guid id)
        {
            bool ready;
            _socketReady.TryGetValue(id, out ready);

            return ready;
        }

        public ICollection<Guid> GetClientIds()
        {
            return _sockets.Keys;
        }

        public ICollection<ClientSocket> GetAll()
        {
            return _sockets.Values;
        }

        public void AddSocket(ClientSocket socket)
        {
            _sockets[socket.Id] = socket;
            _statistics[socket.Id] = new ClientStatistics();
        }

        public void SetStatistics(Guid id, ClientStatistics stats)
        {
            _statistics[id] = stats;
        }

        public async Task RemoveSocket(ClientSocket clientSocket)
        {
            _sockets.TryRemove(clientSocket.Id, out clientSocket);

            if (clientSocket != null)
            {
                await clientSocket.Socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure, statusDescription: "Closed by server", CancellationToken.None);
            }
        }
    }
}
