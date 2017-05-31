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
                MemberInfo method = typeof(Program).GetMethod("Apply", new Type[]{ });

                Apply();
                //Console.WriteLine("return : " + TestIntReturnInTry());
                //Service service = new Service("lynch");
                //service.Say(" hello.", 120);
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

        [Timing]
        public static void Apply() {
            Console.WriteLine("test 2.");
        }

        // Leox.AopBuildTest.Program
        //[Timing]
        public static void Apply12()
        {
            Type arg_22_0 = typeof(Program);
            string arg_22_1 = "Apply";
            Type[] types = new Type[0];
            MemberInfo method = arg_22_0.GetMethod(arg_22_1, types);
            MethodAspectArgs methodAspectArgs = new MethodAspectArgs();
            MAList mAList = new MAList();
            Timing item = method.GetCustomAttributes(typeof(Timing), false)[0] as Timing;
            mAList.Add(item);
            foreach (MethodAspect current in mAList)
            {
                current.OnStart(methodAspectArgs);
            }
            try
            {
                //Program._Apply_();
                foreach (MethodAspect current in mAList)
                {
                    current.OnSuccess(methodAspectArgs);
                }
            }
            catch (Exception ex)
            {
                methodAspectArgs.Exception = ex;
                foreach (MethodAspect current in mAList)
                {
                    current.OnException(methodAspectArgs);
                }
                switch ((int)mAList[0].ExceptionStrategy)
                {
                    case 1:
                        throw ex;
                    case 2:
                        throw;
                }
            }
            foreach (MethodAspect current in mAList)
            {
                current.OnEnd(methodAspectArgs);
            }
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
            Console.WriteLine("timing start" + (args != null && args.Argument != null ? args.Argument[0].ToString() : ""));
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
