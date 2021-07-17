using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace ClusterCore.Utilities
{
    public interface IClientAdapter
    {
        ICollection<Guid> GetClientIds();
        Task<string> EvaluateClient(Guid clientId, object[] parameters, Func<Task> callback = null);
    }
}
