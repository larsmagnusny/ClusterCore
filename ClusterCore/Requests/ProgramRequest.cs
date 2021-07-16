using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterCore.Requests
{
    public class ProgramRequest
    {
        public string Source { get; set; }
        public object[] parameters { get; set; }
    }
}
