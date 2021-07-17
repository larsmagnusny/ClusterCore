using ClusterCore.Requests;
using ClusterCore.Utilities;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class Client
    {
        private static Client Instance;

        public bool ThreadRunning = false;
        private Thread ClientThread;
        private Thread ClientStatisticsThread;

        public Client()
        {
            ParameterizedThreadStart threadStart1 = new ParameterizedThreadStart(ListenForProgram);
            ClientThread = new Thread(threadStart1);

            ParameterizedThreadStart threadStart2 = new ParameterizedThreadStart(SendStatistics);
            ClientStatisticsThread = new Thread(threadStart2);

            Instance = this;
        }

        public void StartThread(object data = null)
        {
            ThreadRunning = true;
            
            ClientThread.Start(data);
            ClientStatisticsThread.Start(data);
        }

        public void StopThread()
        {
            ThreadRunning = false;
        }

        public async void ListenForProgram(object data)
        {
            Console.WriteLine("Waiting 1 sec");
            Thread.Sleep(1000);
            string url = data as string;
            var Uri = new Uri(string.IsNullOrEmpty(url) ? "ws://localhost:5000/program" : string.Concat(url, "/program"));

            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(120);
            Console.WriteLine("Connecting to {0}", url);
            await socket.ConnectAsync(Uri, CancellationToken.None);
            Console.WriteLine("Connected to {0}", url);
            var buffer = new byte[4 * 1024];
            while (!socket.CloseStatus.HasValue && ThreadRunning)
            {
                try
                {
                    await SocketUtilities.SendSocketUntillEnd(socket, "Waiting for program...");
                    string jsonRequest = Encoding.UTF8.GetString(await SocketUtilities.ReadSocketUntillEnd(socket));
                    var request = JsonConvert.DeserializeObject<ProgramRequest>(jsonRequest);

                    var program = new ClusterProgram(request.Source);

                    var entryPoint = program.GetClientEntryPoint();

                    if (entryPoint == null)
                    {
                        await SocketUtilities.SendSocketUntillEnd(socket, "No entrypoint for client found");
                        return;
                    }

                    Task<object> result = entryPoint.GetParameters().Length > 0
                        ? (Task<object>)entryPoint.Invoke(null, new object[] { request.parameters })
                        : (Task<object>)entryPoint.Invoke(null, null);

                    await result;
                    await SocketUtilities.SendSocketUntillEnd(socket, result.Result);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Thread.Sleep(1000);
            }
        }

        public async void SendStatistics(object data)
        {
            string url = data as string;
            var Uri = new Uri(string.IsNullOrEmpty(url) ? "ws://localhost:5000/statistics" : string.Concat(url, "/statistics"));

            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(120);
            Console.WriteLine("Connecting to {0}", url);
            await socket.ConnectAsync(Uri, CancellationToken.None);
            Console.WriteLine("Connected to {0}", url);

            while (!socket.CloseStatus.HasValue && ThreadRunning)
            {
                try
                {
                    Process currentProcess = Process.GetCurrentProcess();
                    Metrics systemInfo = (new MetricsClient()).GetMetrics();

                    await SocketUtilities.SendSocketUntillEnd(socket, systemInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                Thread.Sleep(1000);
            }
        }

        public static Client GetInstance()
        {
            return Instance;
        }
    }
}
