using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Injector
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"E:\Projects\LexoAop\Leox.AopBuildTest\obj\Debug\Leox.AopBuildTest.exe";
            if (args != null && args.Length > 0)
                path = args[0];
            new Injector().Inject(path);
        }
    }
}
