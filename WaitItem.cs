using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadPool
{
    public class WaitItem
    {
        public WaitCallback Works { get; set; }

        public object Context { get; set; }
    }
}
