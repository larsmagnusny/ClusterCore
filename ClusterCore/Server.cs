using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ClusterCore
{
    public class Server
    {
        public Server()
        {
            var tStart = new ThreadStart(GetInput);
            InputThread = new Thread(tStart);
            InputThread.Start();
            ThreadRunning = true;
        }

        public static ClusterExecutionHandler ExecutionHandler { get; set; }

        private static bool ThreadRunning;
        private static Thread InputThread;
        private string source = @"using System;
using System.Text;
using System.Net;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
              
namespace ClusterProgram
{
    public class ClientResult {
        public int Number { get; set; }
        public bool IsOdd { get;set; }
    }

    class Program {
        public static async Task<string> Evaluate(WebSocket socket, object parameters, string source)
        {
            byte[] buffer = new byte[4 * 1024];
            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(source)), WebSocketMessageType.Text, true, CancellationToken.None);
            await socket.ReceiveAsync(buffer, CancellationToken.None);

            return Encoding.UTF8.GetString(buffer);
        }

        static async Task Main(WebSocket[] Clients, string source){
            // Process the results from clients
            var results = await Task.WhenAll(Clients.Select(cl => Evaluate(cl, null, source)));
            
            Console.WriteLine(""Got: {0} results."", results.Length);
        }

        static List<ClientResult> Client(){
            var ret = new List<ClientResult>();
            for(int i = 0; i < 1000; i++){
                ret.Add(new ClientResult { Number = i, IsOdd = i % 2 != 0 });
            }

            return ret;
        }
    }
}
                            ";

        public async void GetInput()
        {
            while (ThreadRunning)
            {
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.CompareTo("run") == 0)
                    await QueueProgram();
            }
        }

        public static void StopThread()
        {
            ThreadRunning = false;
        }

        public async Task QueueProgram()
        {
            ClusterProgram program = new ClusterProgram(source);

            var entryPoint = program.GetServerEntryPoint();
            var clientEntry = program.GetClientEntryPoint();

            if (entryPoint == null)
                Console.WriteLine("Error: Program has no entry point for Server.");

            if (clientEntry == null)
                Console.WriteLine("Error: Program has no entry point for Clients.");

            if (entryPoint == null || clientEntry == null)
                return;

            if (entryPoint.GetParameters().Length > 0)
            {
                Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { ExecutionHandler.GetAllSockets(), program.SourceCode });
                await invokeResult;
            }
            else
                entryPoint.Invoke(null, null);
        }
    }
}
