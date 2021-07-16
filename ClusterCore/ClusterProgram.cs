using ClusterCore.Requests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace ClusterCore
{
    public class ClusterProgram
    {
        public string SourceCode { get; set; }
        private SyntaxTree _syntaxTree;
        private AssemblyLoadContext _loadContext;
        private Assembly _assembly;
        public ClusterProgram(string source)
        {
            SourceCode = source;
            _syntaxTree = CSharpSyntaxTree.ParseText(SourceCode);

            var dotNetCoreDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName: "ClusterProgramCompiled",
                new[] { _syntaxTree },
                new[] {
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Runtime.dll")),
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Collections.dll")),
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Threading.Tasks.dll")),
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "System.Linq.dll")),
                            MetadataReference.CreateFromFile(Path.Combine(dotNetCoreDir, "netstandard.dll")),
                            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                            
                            MetadataReference.CreateFromFile(typeof(WebSocket).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(CancellationToken).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Encoding).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Dns).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(ProgramRequest).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(JsonConvert).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(Stopwatch).GetTypeInfo().Assembly.Location)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            Stream exefStream = new MemoryStream();
            Stream pdbfStream = new MemoryStream();

            var emitResult = compilation.Emit(exefStream, pdbfStream);
            if (!emitResult.Success)
            {
                var failures = emitResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
            }
            else
            {
                exefStream.Seek(0, SeekOrigin.Begin);
                pdbfStream.Seek(0, SeekOrigin.Begin);
                _loadContext = new AssemblyLoadContext("ClusterProgramCompiled", true);
                _assembly = _loadContext.LoadFromStream(exefStream, pdbfStream);
            }
        }

        public MethodInfo GetClientEntryPoint()
        {
            var programClass = _assembly.GetTypes().FirstOrDefault(o => o.IsClass && o.Name.CompareTo("Program") == 0);
            return programClass.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(o => o.Name == "Client");
        }

        public MethodInfo GetServerEntryPoint()
        {
            var programClass = _assembly.GetTypes().FirstOrDefault(o => o.IsClass && o.Name.CompareTo("Program") == 0);
            return programClass.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(o => o.Name == "Main");
        }
    }
}
