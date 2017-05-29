using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Aop
{
    public enum ExceptionStrategy
    {
        UnThrow,
        /// <summary>
        /// throw ex; 
        /// </summary>
        ThrowNew,
        /// <summary>
        /// throw; //保存有异常跟踪链
        /// </summary>
        ReThrow
    }
}
