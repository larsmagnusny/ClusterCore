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
    public class ClusterExecutionHandler : WebSocketHandler
    {
        public static ConcurrentDictionary<Guid, ConcurrentQueue<ClusterProgram>> QueuedPrograms = new ConcurrentDictionary<Guid, ConcurrentQueue<ClusterProgram>>();

        public ClusterExecutionHandler(SocketManager socketManager) : base(socketManager)
        {

        }

        public WebSocket[] GetAllSockets()
        {
            return WebSocketConnectionManager.GetAll().Values.ToArray();
        }

        public override async Task ReceiveAsync(WebSocket socket, byte[] buffer)
        {
            // Recieve results from execution...
            Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));

            Guid id = WebSocketConnectionManager.GetId(socket);

            ConcurrentQueue<ClusterProgram> programQueue;
            QueuedPrograms.TryGetValue(id, out programQueue);

            if(programQueue == null)
            {
                programQueue = new ConcurrentQueue<ClusterProgram>();
                QueuedPrograms[id] = programQueue;
            }

            while (true)
            {
                while (!programQueue.IsEmpty)
                {
                    

                    Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));
                }

                Thread.Sleep(200);
            }
        }
    }
}
