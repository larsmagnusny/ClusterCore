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
    public static class Client
    {
        public static bool ThreadRunning = false;
        private static Thread ClientThread = new Thread(ListenForProgram);
        private static Thread ClientStatisticsThread = new Thread(SendStatistics);

        public static void StartThread(object data = null)
        {
            ThreadRunning = true;
            ClientThread.Start(data);
            //ClientStatisticsThread.Start(data);
        }

        public static void StopThread()
        {
            ThreadRunning = false;
        }

        public static async void ListenForProgram(object data)
        {
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

                    string strRes = JsonConvert.SerializeObject(result.Result, Formatting.None);
                    await SocketUtilities.SendSocketUntillEnd(socket, strRes);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Thread.Sleep(1000);
            }
        }

        public static async void SendStatistics(object data)
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
                    ClientStatistics systemInfo = new ClientStatistics
                    {
                        Bytes = currentProcess.WorkingSet64,
                        TotalProcessorTime = currentProcess.TotalProcessorTime.TotalSeconds
                    };

                    string statsString = JsonConvert.SerializeObject(systemInfo, Formatting.None);

                    await SocketUtilities.SendSocketUntillEnd(socket, statsString);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
