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
        public static ConcurrentQueue<ClusterProgram> QueuedPrograms = new ConcurrentQueue<ClusterProgram>();
        public ClusterExecutionHandler(SocketManager socketManager) : base(socketManager)
        {

        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            // Recieve results from execution...
            Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));

            while (true)
            {
                if (!QueuedPrograms.IsEmpty)
                {
                    ClusterProgram program;
                    QueuedPrograms.TryDequeue(out program);

                    if (program == null)
                    {
                        Console.Write("Error: There was an empty program queued.");
                        continue;
                    }

                    var entryPoint = program.GetServerEntryPoint();
                    var clientEntry = program.GetClientEntryPoint();

                    if (entryPoint == null)
                        Console.WriteLine("Error: Program has no entry point for Server.");

                    if(clientEntry == null)
                        Console.WriteLine("Error: Program has no entry point for Clients.");

                    if (entryPoint == null || clientEntry == null)
                        continue;

                    if (entryPoint.GetParameters().Length > 0)
                    {
                        Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { WebSocketConnectionManager.GetAll().Select(o => o.Value).ToArray(), program.SourceCode });
                        await invokeResult;
                    }
                    else
                        entryPoint.Invoke(null, null);

                    Console.WriteLine(Encoding.UTF8.GetString(buffer).Trim('\0'));
                    Thread.Sleep(100);
                }

                Thread.Sleep(200);
            }
        }
    }
}
