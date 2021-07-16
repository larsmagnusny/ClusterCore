﻿using System;
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
        
        static async Task Main(ClientSocket[] Clients, string source)
        {
            long maxPrime = 1000;
            long numClients = Clients.Length;

            long itemsPerClient = maxPrime / numClients;

            long lastRange = itemsPerClient;

            if(itemsPerClient * numClients < maxPrime)
            {
                lastRange += maxPrime - itemsPerClient * numClients;
            }

            List<Task<string>> Tasks = new List<Task<string>>();

            long from, to;
            // Process the results from clients
            for(int i = 0; i < Clients.Length; i++)
            {
                var clientSocket = Clients[i];
                from = i * itemsPerClient;
                if (i < Clients.Length - 1)
                {
                    to = from + itemsPerClient;
                    Tasks.Add(SocketUtilities.EvaluateClient(clientSocket.Socket, new object[] { from, to }, source, () => { Console.WriteLine("{0} has completed its task.", clientSocket.Id); return Task.CompletedTask; }));
                }
                else
                {
                    to = from + lastRange;
                    Tasks.Add(SocketUtilities.EvaluateClient(clientSocket.Socket, new object[] { from, to }, source, () => { Console.WriteLine("{0} has completed its task.", clientSocket.Id); return Task.CompletedTask; }));
                }

                Console.WriteLine("{0} is processing range {1}-{2}", clientSocket.Id, from, to);
            }

            var taskResults = await Task.WhenAll(Tasks);

            List<ClientResult> results = new List<ClientResult>();

            foreach(var item in taskResults)
            {
                List<ClientResult> arr = JsonConvert.DeserializeObject<List<ClientResult>>(item);
                results.AddRange(arr);
            }

            foreach(var item in results)
            {
                Console.WriteLine("{0} is {1}", item.Number, item.IsPrime ? "prime" : "not prime");
            }

            Console.WriteLine("Got: {0} results.", taskResults.Length);
        }

        static List<ClientResult> Client(object[] parameters)
        {
            int from = Convert.ToInt32(parameters[0]);
            int to = Convert.ToInt32(parameters[1]);
            var ret = new List<ClientResult>();
            for (int i = from; i < to; i++)
            {
                ret.Add(new ClientResult { Number = i, IsPrime = IsPrime(i) });
            }

            return ret;
        }

        static bool IsPrime(int i)
        {
            if (i == 1)
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
