using ClusterCore.Utilities;
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
                    var client = new Client();
                    client.StartThread(args.Length > 1 ? args[1] : null);

                    while (client.ThreadRunning)
                    {
                        Thread.Sleep(250);
                    }
                }
                if (args[0].CompareTo("host") == 0)
                {
                    _ = new Server();
                    var host = new WebHostBuilder()
                        .UseUrls("http://*:5000", "https://*:5001")
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseStartup<Startup>()
                        .Build();

                    host.Run();
                }
            }
            else
            {
                var client = new Client();
                client.StartThread(args.Length > 1 ? args[1] : null);

                _ = new Server();
                var host = new WebHostBuilder()
                    .UseUrls("http://*:5000", "https://*:5001")
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .Build();

                host.Run();
            }
        }
    }
}
