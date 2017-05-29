using Leox.Aop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Leox.AopBuildTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Service service = Test1();

            //foreach (var item in typeof(AopIncepter).GetMethods()) {
            //    var aa = item.GetCustomAttributes(typeof(AopIncepter), false);
            //}
            try
            {
                //Console.WriteLine("return : " + TestIntReturnInTry());
                Service service = new Service("lynch");
                service.Say(" hello.", 120);
               // Console.WriteLine(result ? "ok" : "faile");
                //Console.WriteLine("my build test");
                Console.ReadKey();
            }
            catch (OutOfMemoryException ex)
            {
                throw ex;
            }
            catch (StackOverflowException sex) {
                throw;
            }
            catch (Exception eax)
            {
                Console.WriteLine("main exception : " + eax.Message+ "  " + eax.StackTrace);

            }

            Console.ReadKey();
        }

    


        static int TestIntReturnInTry()
        {
            int i = 0;
            try
            {
                return i;
            }
            finally
            {
                i = 2;
            }
        }

        private static Service Test1()
        {
            int a = 123;
            Service service = new Service("lynch");
            var b = service.GetNumber();
            var result = service.Add(a, b);
            return service;
        }
    }
    class Timing : MethodAspect
    {
        public override void OnException(MethodAspectArgs args)
        {
            Console.WriteLine("timing on exception: " + args.Exception.Message);
        }
        public override void OnStart(MethodAspectArgs args)
        {
            if (args != null && args.Argument != null) {
                foreach (var item in args.Argument)
                {
                    Console.Write(item.ToString() + "  ");
                }
            }
            Console.WriteLine("timing start" + (args != null ? args.Argument[0].ToString() : ""));
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("timing end");
        }

        public override void OnSuccess(MethodAspectArgs args)
        {
            Console.WriteLine("timing success : " + args.ReturnValue.ToString());
        }
    }
    class Log : MethodAspect
    {
        public override void OnException(MethodAspectArgs args)
        {
            Console.WriteLine("log on exception: " + args.Exception.Message);
        }
        public override void OnStart(MethodAspectArgs args)
        {
            Console.WriteLine("log start");
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("log end");
        }

        public override void OnSuccess(MethodAspectArgs args)
        {
            Console.WriteLine("log success : " + args.ReturnValue.ToString());
        }
    }
    
}
