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
        private static byte startByte = (byte)'a';
        private static byte endByte = (byte)'z';

        public class CrackResult
        {
            public string Password { get; set; }
            public bool Cracked { get; set; }
        }

        public class Password
        {
            public byte[] arr { get; set; }

            private int i;

            public void Next()
            {
                for(i = arr.Length-1; i >= 0; i--)
                {
                    if (arr[i] + 1 > endByte)
                    {
                        arr[i] = startByte;

                        if (i == 0)
                        {
                            byte[] tmp = new byte[arr.Length + 1];
                            Array.Copy(arr, 0, tmp, 1, arr.Length);
                            tmp[0] = startByte;
                            arr = tmp;
                            return;
                        }
                        
                        continue;
                    }

                    arr[i]++;
                    return;
                }
            }
        }

        //private static MD5 md5 = MD5.Create();
        //private static object md5lock = new object();

        static async Task Main(IClientAdapter clientAdapter, string[] args)
        {
            List<Task<string>> tasks = new List<Task<string>>();

            var clients = clientAdapter.GetClientIds();

            int charspace = endByte - startByte;

            int delta = (endByte - startByte) / clients.Count;

            int lastDelta = delta;

            if (delta * clients.Count > charspace)
                lastDelta -= charspace - delta * clients.Count;
            else if(delta * clients.Count < charspace)
                lastDelta += delta * clients.Count - charspace;

            int max = int.Parse(args[0]);
            byte[] hashToCrack = StringToByteArray(args[1]);

            for (int i = 0; i < clients.Count; i++) {
                var clientId = clients.ElementAt(i);
                int startMax = i > 0 ? max : 1;

                byte[] start = new byte[startMax];
                byte[] end = new byte[max];

                Array.Fill(start, startByte);
                Array.Fill(end, endByte);

                if(i > 0)
                    start[0] = (byte)(startByte + i * delta);

                if (i < clients.Count - 1)
                    end[0] = (byte)(startByte + i * delta + delta);
                else
                    end[0] = (byte)(startByte + i * delta + lastDelta);

                tasks.Add(clientAdapter.EvaluateClient(clientId, new object[]
                {
                    hashToCrack,
                    start,
                    end
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

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private static void Crack(object data)
        {
            var p = (CrackParameters)data;

            Password curItem = new Password
            {
                arr = new byte[p.start.Length]
            };

            Array.Copy(p.start, curItem.arr, p.start.Length);

            int counter = 0;

            while (!SafeEquals(curItem.arr, p.end) && !p.cts.IsCancellationRequested)
            {
                byte[] computedHash = p.md5.ComputeHash(curItem.arr);
                if (SafeEquals(computedHash, p.hashToCrack))
                {
                    p.result.Cracked = true;
                    p.result.Password = Encoding.ASCII.GetString(curItem.arr);
                    break;
                }
                //if (counter++ == 10000)
                //{
                //    counter = 0;
                //    Console.WriteLine(Encoding.ASCII.GetString(curItem.arr));
                //}
                curItem.Next();
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
            int charSpace = start.Length != end.Length ? (end[0] - startByte) : (end[0] - start[0]);

            int delta = charSpace / numThreads;

            int lastDelta = delta;

            if (delta * numThreads > charSpace)
                lastDelta -= delta * numThreads - charSpace;
            else if (delta * numThreads < endByte)
                lastDelta += charSpace - delta * numThreads;

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
                int startLength = start.Length;

                if (start.Length != end.Length && i > 0)
                {
                    startLength = end.Length;
                    start = new byte[startLength];
                    Array.Fill(start, startByte, 0, startLength);
                }

                byte[] hcrack = new byte[hashToCrack.Length];
                byte[] startR = new byte[startLength];
                byte[] endR = new byte[end.Length];

                Array.Copy(hashToCrack, hcrack, hashToCrack.Length);
                Array.Copy(start, startR, startLength);
                Array.Copy(end, endR, end.Length);

                byte b_start = start[0];
                byte b_end = end[0];

                startR[0] = (byte)(b_start + delta*i);

                

                if (i < numThreads - 1)
                    endR[0] = (byte)(b_start + delta * i + delta);
                else
                    endR[0] = (byte)(b_start + delta * i + lastDelta);

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

        private static bool SafeEquals(byte[] strA, byte[] strB)
        {
            int length = strA.Length;
            if (length != strB.Length)
            {
                return false;
            }
            for (int i = 0; i < length; i++)
            {
                if (strA[i] != strB[i]) return false;
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
