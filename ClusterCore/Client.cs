using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
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
            while (socket.State == WebSocketState.Open && ThreadRunning)
            {
                try
                {
                    Console.WriteLine("Waiting for something to do...");
                    await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    string source = Encoding.UTF8.GetString(buffer).Trim('\0');
                    Console.WriteLine("Recieved: {0}", source);

                    var syntaxTree = CSharpSyntaxTree.ParseText(source);

                    var dotNetCoreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

                    CSharpCompilation compilation = CSharpCompilation.Create(assemblyName: "ClusterProgramCompiled",
                        new[] { syntaxTree },
                        new[] {
                            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Dns).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll"))
                        },
                        new CSharpCompilationOptions(OutputKind.ConsoleApplication));

                    FileStream exefStream = File.OpenWrite("ClusterProgramCompiled.dll");
                    FileStream pdbfStream = File.OpenWrite("ClusterProgramCompiled.pdb");
                    FileStream runtimeConfig = File.OpenWrite("ClusterProgramCompiled.runtimeconfig.json");
                    GenerateRuntimeConfig(runtimeConfig);
                    runtimeConfig.Close();

                    var emitResult = compilation.Emit(exefStream, pdbfStream);
                    if (!emitResult.Success)
                    {
                        var failures = emitResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach(Diagnostic diagnostic in failures)
                        {
                            Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        }
                    }
                    else
                    {
                        exefStream.Close();
                        pdbfStream.Close();
                        var proc = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "dotnet",
                                Arguments = "ClusterProgramCompiled.dll",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            }
                        };

                        proc.Start();

                        while (!proc.StandardOutput.EndOfStream)
                        {
                            string line = proc.StandardOutput.ReadLine();
                            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(line)), WebSocketMessageType.Binary, proc.StandardOutput.EndOfStream, CancellationToken.None);
                        }

                        Process.Start("dotnet", "ClusterProgramCompiled.dll");
                    }

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
