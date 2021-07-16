using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore.Utilities
{
    public class ClientSocket
    {
        public Guid Id { get; set; }
        public WebSocket Socket { get; set; }
    }
}
