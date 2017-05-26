using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    public interface IMethodAspect
    {
        void OnStart();
        void OnEnd();
        void OnException();
    }
}
