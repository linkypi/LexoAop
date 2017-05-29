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

        private ExceptionStrategy _exceptionStrategy = ExceptionStrategy.ReThrow;
        public ExceptionStrategy ExceptionStrategy
        {
            get { return _exceptionStrategy; }
            set
            {
                _exceptionStrategy = value;
            }
        }
        public MethodAspect() { }

        public virtual void OnStart(MethodAspectArgs args)
        {
        }

        public virtual void OnEnd(MethodAspectArgs args)
        {
        }
        public virtual void OnSuccess(MethodAspectArgs args)
        {
        }
        public virtual void OnException(MethodAspectArgs args)
        {
        }
    }
}
