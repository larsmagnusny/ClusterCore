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
        private ConcurrentDictionary<Guid, WebSocket> _sockets = new ConcurrentDictionary<Guid, WebSocket>();
        private ConcurrentDictionary<WebSocket, Guid> _ids = new ConcurrentDictionary<WebSocket, Guid>();
        private ConcurrentDictionary<Guid, ClientStatistics> _statistics = new ConcurrentDictionary<Guid, ClientStatistics>();
        private ConcurrentDictionary<Guid, bool> _socketReady = new ConcurrentDictionary<Guid, bool>();

        public WebSocket GetSocketById(Guid id)
        {
            return _sockets[id];
        }

        public Guid GetId(WebSocket socket)
        {
            return _ids[socket];
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

        public ConcurrentDictionary<Guid, WebSocket> GetAll()
        {
            return _sockets;
        }

        public void AddSocket(WebSocket socket)
        {
            Guid newId = Guid.NewGuid();
            _sockets[newId] = socket;
            _ids[socket] = newId;
            _statistics[newId] = new ClientStatistics();
        }

        public void SetStatistics(Guid id, ClientStatistics stats)
        {
            _statistics[id] = stats;
        }

        public async Task RemoveSocket(Guid id)
        {
            WebSocket socket;

            _sockets.TryRemove(id, out socket);

            if (socket != null)
            {
                _ids.TryRemove(socket, out id);
                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure, statusDescription: "Closed by server", CancellationToken.None);
            }
        }
    }
}
