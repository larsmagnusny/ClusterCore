using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;

namespace ClusterCore
{
    class Program
    {
        private static Thread clientThread = null;
        static void Main(string[] args)
        {
            clientThread = new Thread(Client.Run)
            {
                Priority = ThreadPriority.Normal
            };

            clientThread.Start(args);

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
