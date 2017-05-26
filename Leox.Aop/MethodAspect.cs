using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodAspect : Attribute, IMethodAspect
    {
        public int Order { get; set; }
        public MethodAspect() { }

        public virtual void OnStart()
        {
        }

        public virtual void OnEnd()
        {
        }

        public virtual void OnException()
        {
        }
    }
}
