using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async void GetInput()
        {
            while (ThreadRunning)
            {
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                string[] args = input.Split(' ');

                if (args[0].CompareTo("run") == 0 && args.Length == 2)
                {
                    if (args.Length != 2)
                    {
                        Console.WriteLine("Invalid number of parameters");
                        continue;
                    }

                    string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                    string filePath = Path.Combine(currentPath, args[1]);

                    if (File.Exists(filePath))
                        QueueProgram(filePath);
                    else
                        Console.Write("Error: file does not exist - {0}", filePath);
                }
                else
                    Console.WriteLine("Unrecognized command");
            }
        }

        public static void StopThread()
        {
            ThreadRunning = false;
        }

        public async Task QueueProgram(string programPath)
        {
            try
            {
                FileStream fileStream = File.OpenRead(programPath);
                byte[] sourceBytes = new byte[fileStream.Length];
                fileStream.Read(sourceBytes, 0, sourceBytes.Length);
                fileStream.Close();
                ClusterProgram program = new ClusterProgram(Encoding.UTF8.GetString(sourceBytes));

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
                    var clientSockets = ExecutionHandler.GetAllSockets();
                    Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { clientSockets.ToArray(), program.SourceCode });
                    await invokeResult;

                    ExecutionHandler.ResetSockets(clientSockets);
                }
                else
                    entryPoint.Invoke(null, null);
            } 
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
