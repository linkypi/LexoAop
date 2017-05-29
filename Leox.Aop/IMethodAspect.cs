using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    public interface IMethodAspect
    {
        void OnStart(MethodAspectArgs args);
        void OnEnd(MethodAspectArgs args);
        void OnSuccess(MethodAspectArgs args);
        void OnException(MethodAspectArgs args);
    }
}
