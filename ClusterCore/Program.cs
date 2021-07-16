using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;

namespace ClusterCore
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].CompareTo("join") == 0)
                {
                    Client.StartThread(args.Length > 1 ? args[1] : null);

                    while (Client.ThreadRunning)
                    {
                        Thread.Sleep(250);
                    }
                }
                if (args[0].CompareTo("host") == 0)
                {
                    var server = new Server();
                    var host = new WebHostBuilder()
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .Build();

                    host.Run();
                }
            }
            else
            {
                Client.StartThread();

                var server = new Server();
                var host = new WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .Build();

                host.Run();
            }
        }
    }
}
