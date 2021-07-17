using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore
{
    public class Metrics
    {
        public double CPULoad;
        public double Total;
        public double Used;
        public double Free;
    }

    public class MetricsClient
    {
        
        public Metrics GetMetrics()
        {
            if (IsUnix())
            {
                return GetUnixMetrics();
            }

            return GetWindowsMetrics();
        }

        private bool IsUnix()
        {
            var isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            return isUnix;
        }

        private int GetCPULoadWindows()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "cpu get LoadPercentage /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var line = output.Trim();
            var cpu = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
            return int.Parse(cpu[1]);
        }

        private double GetCPULoadLinux()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"top -bn 1\" |grep \"Cpu(s)\" | awk '{print ($2+$6+$4+$12+$14+$16)/2}'";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var cpu = output;

            return double.Parse(cpu);
        }

        private Metrics GetWindowsMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);
            double total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            double free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            var metrics = new Metrics
            {
                CPULoad = GetCPULoadWindows(),
                Total = total,
                Free = free,
                Used = total - free
            };
            return metrics;
        }

        private Metrics GetUnixMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new Metrics
            {
                CPULoad = GetCPULoadLinux(),
                Total = double.Parse(memory[1]),
                Used = double.Parse(memory[2]),
                Free = double.Parse(memory[3])
            };

            return metrics;
        }
    }
}
