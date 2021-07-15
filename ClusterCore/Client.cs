using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ClusterCore
{
    public static class Client
    {
        public static bool ThreadRunning = false;
        private static Thread ClientThread = new Thread(Client.Run);

        public static void StartThread(object data = null)
        {
            ThreadRunning = true;
            ClientThread.Start(data);
        }

        public static void StopThread()
        {
            ThreadRunning = false;
        }

        private static void GenerateRuntimeConfig(Stream stream)
        {
            using (var writer = new Utf8JsonWriter(
                stream,
                new JsonWriterOptions() { Indented = true }
            ))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("runtimeOptions");
                writer.WriteStartObject("framework");
                writer.WriteString("name", "Microsoft.AspNetCore.App");
                writer.WriteString(
                    "version",
                    RuntimeInformation.FrameworkDescription.Replace(".NET ", "")
                );
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
        }

        public static async void Run(object data)
        {
            string url = data as string;
            var Uri = new Uri(string.IsNullOrEmpty(url) ? "ws://localhost:5000/ws" : string.Concat(url, "/ws"));

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
                    Console.WriteLine("Waiting for something to do...");
                    await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    string source = Encoding.UTF8.GetString(buffer).Trim('\0');

                    var program = new ClusterProgram(source);

                    var entryPoint = program.GetClientEntryPoint();

                    if (entryPoint == null)
                    {
                        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("No entrypoint for client found")), WebSocketMessageType.Text, true, CancellationToken.None);
                        return;
                    }

                    var result = entryPoint.GetParameters().Length > 0
                        ? entryPoint.Invoke(null, new object[] { null })
                        : entryPoint.Invoke(null, null);

                    string strRes = JsonConvert.SerializeObject(result, Formatting.None);
                    await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(strRes)), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                Thread.Sleep(1000);
            }
        }
    }
}
