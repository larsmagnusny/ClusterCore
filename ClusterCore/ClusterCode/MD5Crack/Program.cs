using ClusterCore.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterCore.ClusterCode.MD5Crack
{
    public class Program
    {
        private static int numThreads = 4;
        private static byte startByte = (byte)'A';
        private static byte endByte = (byte)'z';

        public class CrackResult
        {
            public string Password { get; set; }
            public bool Cracked { get; set; }
        }

        public class Password
        {
            public byte[] arr { get; set; }

            public void Next(int offset)
            {
                int lastIndex = arr.Length - 1 - offset;

                if (lastIndex < 0)
                {
                    byte[] newArr = new byte[offset + 1];
                    Array.Copy(arr, 0, newArr, 1, arr.Length);
                    arr = newArr;
                    arr[0] = (byte)(startByte + 1);
                    return;
                }

                if (arr[lastIndex] + 1 > endByte)
                {
                    Next(offset + 1);
                    arr[lastIndex] = startByte;

                    return;
                }

                arr[lastIndex]++;
            }
        }

        //private static MD5 md5 = MD5.Create();
        //private static object md5lock = new object();

        static async Task Main(IClientAdapter clientAdapter, string[] args)
        {
            List<Task<string>> tasks = new List<Task<string>>();

            var clients = clientAdapter.GetClientIds();

            int delta = (endByte - startByte) / clients.Count;

            for (int i = 0; i < clients.Count; i++) {
                var clientId = clients.ElementAt(i);
                tasks.Add(clientAdapter.EvaluateClient(clientId, new object[]
                {
                    new byte[]{ 0x16, 0xd7, 0xa4, 0xfc, 0xa7, 0x44, 0x2d, 0xda, 0x3a, 0xd9, 0x3c, 0x9a, 0x72, 0x65, 0x97, 0xe4 },
                    new byte[]{ (byte)(startByte + i*delta), startByte, startByte, startByte, startByte, startByte, startByte, startByte },
                    new byte[]{ (byte)(startByte + i*delta + (i+1)*delta), endByte, endByte, endByte, endByte, endByte, endByte, endByte }
                }));
            }

            var results = Task.WhenAll(tasks);

            foreach (var item in results.Result)
                Console.WriteLine(item);
        }

        public class CrackParameters
        {
            public byte[] hashToCrack { get; set; }
            public byte[] start { get; set; }
            public byte[] end { get; set; }
            public MD5 md5 { get; set; }
            public CrackResult result { get; set; }
            public CancellationTokenSource cts { get; set; }
        }

        private static void Crack(object data)
        {
            var p = (CrackParameters)data;

            Password curItem = new Password
            {
                arr = new byte[p.start.Length]
            };

            Array.Copy(p.start, curItem.arr, p.start.Length);

            while (!Equals(curItem.arr, p.end) && !p.cts.IsCancellationRequested)
            {
                if (Equals(p.md5.ComputeHash(curItem.arr), p.hashToCrack))
                {
                    p.result.Cracked = true;
                    p.result.Password = Encoding.ASCII.GetString(curItem.arr);
                    break;
                }
                
                curItem.Next(0);
            }

            if (p.result.Cracked)
                p.cts.Cancel();

            return;
        }

        static async Task<object> Client(object[] parameters)
        {
            byte[] hashToCrack = Convert.FromBase64String(parameters[0] as string);
            byte[] start = Convert.FromBase64String(parameters[1] as string);
            byte[] end = Convert.FromBase64String(parameters[2] as string);

            

            int delta = (end[0] - start[0]) / numThreads;

            bool found = false;
            string password = null;

            var cts = new CancellationTokenSource();
            List<Thread> tasks = new List<Thread>();
            List<CrackResult> results = new List<CrackResult>();

            for(int i = 0; i < numThreads; i++)
            {
                results.Add(new CrackResult());
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for(int i = 0; i < numThreads; i++)
            {
                byte[] hcrack = new byte[hashToCrack.Length];
                byte[] startR = new byte[start.Length];
                byte[] endR = new byte[end.Length];

                Array.Copy(hashToCrack, hcrack, hashToCrack.Length);
                Array.Copy(start, startR, start.Length);
                Array.Copy(end, endR, end.Length);

                startR[0] = (byte)(startR[0] + (delta * i));
                endR[0] = (byte)(endR[0] + (delta * i + delta * (i +1)));

                var thread = new Thread(Crack);
                tasks.Add(thread);
                thread.Start(new CrackParameters
                {
                    hashToCrack = hcrack,
                    start = startR,
                    end = endR,
                    result = results[i],
                    md5 = MD5.Create(),
                    cts = cts
                });
            }

            bool done = false;
            while (!done)
            {
                int aliveCounter = 0;
                foreach(var item in tasks)
                {
                    if (item.IsAlive)
                        aliveCounter++;
                }

                if (aliveCounter == 0)
                    done = true;

                Thread.Sleep(50);
            }

            //var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            stopwatch.Stop();
            Console.WriteLine("Took: {0}s to crack...", stopwatch.ElapsedMilliseconds / 1000);
            foreach(var item in results)
            {
                if (item.Cracked)
                {
                    found = true;
                    password = item.Password;
                    break;
                }
            }

            return new CrackResult { Cracked = found, Password = password };
        }

        public static bool Equals(byte[] first, byte[] second)
        {
            if (first.Length != second.Length)
                return false;

            for(int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                    return false;
            }

            return true;
        }

        public static string ByteArrToString(byte[] input)
        {
            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                sb.Append(input[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
