using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    public class MethodAspectArgs
    {
        public object[] Argument { get; set; }
        public Exception Exception { get; set; }

        public MethodAspectArgs() { }
        public MethodAspectArgs(Object[] args) {
            this.Argument = args;
        }
    }
}
