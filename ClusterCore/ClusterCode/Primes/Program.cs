using System;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ClusterCore.Utilities;
using System.Linq;
using System.Diagnostics;

namespace ClusterProgram
{
    public class ClientResult
    {
        public int Number { get; set; }
        public bool IsPrime { get; set; }
    }

    class Program
    {
        
        static async Task Main(IClientAdapter clientAdapter, string[] args)
        {
            var clientIds = clientAdapter.GetClientIds();
            long maxPrime = 10000;
            
            if(args.Length > 0)
                long.TryParse(args[0], out maxPrime);
            
            long numClients = clientIds.Count;

            long itemsPerClient = maxPrime / numClients;

            long lastRange = itemsPerClient;

            if(itemsPerClient * numClients < maxPrime)
            {
                lastRange += maxPrime - itemsPerClient * numClients;
            }

            List<Task<string>> Tasks = new List<Task<string>>();

            long from, to;
            // Process the results from clients
            for(int i = 0; i < clientIds.Count; i++)
            {
                var clientId = clientIds.ElementAt(i);
                from = i * itemsPerClient;
                if (i < clientIds.Count - 1)
                {
                    to = from + itemsPerClient;
                    Tasks.Add(clientAdapter.EvaluateClient(clientId, new object[] { from, to }, () => { Console.WriteLine("{0} has completed its task.", clientId); return Task.CompletedTask; }));
                }
                else
                {
                    to = from + lastRange;
                    Tasks.Add(clientAdapter.EvaluateClient(clientId, new object[] { from, to }, () => { Console.WriteLine("{0} has completed its task.", clientId); return Task.CompletedTask; }));
                }

                Console.WriteLine("{0} is processing range {1}-{2}", clientId, from, to);
            }

            var taskResults = await Task.WhenAll(Tasks);

            List<ClientResult> results = new List<ClientResult>();

            foreach(var item in taskResults)
            {
                List<ClientResult> arr = JsonConvert.DeserializeObject<List<ClientResult>>(item);
                results.AddRange(arr);
            }

            results = results.OrderBy(o => o.Number).ToList();

            foreach(var item in results)
            {
                Console.WriteLine("{0} is {1}", item.Number, item.IsPrime ? "prime" : "not prime");
            }

            Console.WriteLine("Got: {0} results.", taskResults.Length);
        }

        static async Task<List<ClientResult>> IsPrimeList(int from, int to)
        {
            var ret = new List<ClientResult>();
            for (int i = from; i < to; i++)
            {
                ret.Add(new ClientResult { Number = i, IsPrime = IsPrime(i) });
            }

            return ret;
        }

        // Entry point for client
        static async Task<object> Client(object[] parameters)
        {
            int from = Convert.ToInt32(parameters[0]);
            int to = Convert.ToInt32(parameters[1]);

            int delta = to - from;

            int deltaSplit = delta / 4;

            int lastRange = deltaSplit;

            if (deltaSplit * 4 < delta)
            {
                lastRange += delta - deltaSplit * 4;
            }

            List<Task<List<ClientResult>>> taskList = new List<Task<List<ClientResult>>>();

            for(int i = 0; i < 4; i++)
            {
                int f = from + deltaSplit * i, t;
                if (i < 3)
                    t = from + deltaSplit * i + deltaSplit;
                else
                    t = f + lastRange;

                taskList.Add(Task.Run(() => IsPrimeList(f, t)));
            }

            var taskResults = await Task.WhenAll(taskList);
            var ret = new List<ClientResult>();
            foreach (var item in taskResults)
            {
                ret.AddRange(item);
            }

            return ret;
        }

        static bool IsPrime(int i)
        {
            if (i <= 1)
                return false;
            if (i == 2)
                return true;

            for (int j = 3; j < i / 2; j++)
                if (i % j == 0)
                    return false;

            return true;
        }
    }
}
