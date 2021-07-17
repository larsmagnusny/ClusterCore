using ClusterCore.Utilities;
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

        private static Server Instance { get; set; }

        private bool ThreadRunning;
        private Thread InputThread;

        public async void GetInput()
        {
            while (ThreadRunning)
            {
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                string[] args = input.Split(' ');

                if (args[0].CompareTo("run") == 0)
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Invalid number of parameters");
                        continue;
                    }

                    string currentPath = AppDomain.CurrentDomain.BaseDirectory;
                    string filePath = Path.Combine(currentPath, args[1]);

                    string[] p_args = args.Skip(2).ToArray();

                    if (File.Exists(filePath))
                        QueueProgram(filePath, p_args);
                    else
                        Console.Write("Error: file does not exist - {0}", filePath);
                }
                else
                    Console.WriteLine("Unrecognized command");
            }
        }

        public void StopThread()
        {
            ThreadRunning = false;
        }

        public async Task QueueProgram(string programPath, string[] args)
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

                    var parameters = entryPoint.GetParameters();
                    int numParameters = parameters.Length;

                    if (numParameters == 0)
                    {
                        Task invokeResult = (Task)entryPoint.Invoke(null, null);
                        await invokeResult;
                    }
                    else if (numParameters == 1)
                    {
                        Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { new ClientAdapter(ExecutionHandler.ConnectionManager, program) });
                        await invokeResult;
                    }
                    else if (numParameters == 2) {
                        Task invokeResult = (Task)entryPoint.Invoke(null, new object[] { new ClientAdapter(ExecutionHandler.ConnectionManager, program), args });
                        await invokeResult;
                    }

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

        public static Server GetInstance()
        {
            return Instance;
        }
    }
}
