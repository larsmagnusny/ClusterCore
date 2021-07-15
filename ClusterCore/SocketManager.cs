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

        public WebSocket GetSocketById(Guid id)
        {
            return _sockets[id];
        }

        public Guid GetId(WebSocket socket)
        {
            return _ids[socket];
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
