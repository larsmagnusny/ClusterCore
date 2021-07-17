using ClusterCore.Requests;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore.Utilities
{
    public class ClientAdapter : IClientAdapter
    {
        private readonly SocketManager _socketManager;
        private readonly ClusterProgram _clusterProgram;
        private byte[] buffer;
        private byte[] intBuf;

        public ClientAdapter(SocketManager socketManager = null, ClusterProgram clusterProgram = null)
        {
            _socketManager = socketManager;
            _clusterProgram = clusterProgram;
            buffer = new byte[4096];
            intBuf = new byte[4];
        }

        public ICollection<Guid> GetClientIds()
        {
            return _socketManager.GetClientIds();
        }

        public async Task<string> EvaluateClient(Guid clientId, object[] parameters, Func<Task> callback = null)
        {
            var clientSocket = _socketManager.GetSocketById(clientId);
            var programObj = new ProgramRequest { Source = _clusterProgram.SourceCode, parameters = parameters };

            await SocketUtilities.SendSocketUntillEnd(clientSocket.Socket, programObj);
            string ret = Encoding.UTF8.GetString(await SocketUtilities.ReadSocketUntillEnd(clientSocket.Socket));

            if (callback != null)
                await callback.Invoke();

            return ret;
        }
    }
}
