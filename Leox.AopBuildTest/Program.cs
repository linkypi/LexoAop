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

            Service service = new Service("lynch");
            service.Say(" say hello.",120);
            Console.WriteLine("my build test");
            Console.ReadKey();
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
        public override void OnStart(MethodAspectArgs args)
        {
            if (args != null && args.Argument != null) {
                foreach (var item in args.Argument)
                {
                    Console.WriteLine(item.ToString());
                }
            }
            Console.WriteLine("timing start" + args != null ? args.Argument[0].ToString() : "");
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("timing end");
        }
    }
    class Log : MethodAspect
    {
        public override void OnStart(MethodAspectArgs args)
        {
            Console.WriteLine("log start");
        }

        public override void OnEnd(MethodAspectArgs args)
        {
            Console.WriteLine("log end");
        }
    }
    class Service
    {
        public string Name { get; set; }
        public Service() { }
        public Service(string name)
        {
            this.Name = name;
        }

        public int GetNumber()
        {
            return 100;
        }

        public void Test(string words)
        {
            var method = typeof(Service).GetMethod("Say", new Type[] { typeof(string) });
            //AopIncepter incepter = typeof(Service).GetMethod("Say", new Type[] { typeof(string) })
            //  .GetCustomAttributes(typeof(AopIncepter), false)[0] as AopIncepter;
            //incepter.OnStart();
            //this._Say_(words);
            //incepter.OnEnd();
        }

        //[AopIncepter]
        //private void _Say_(string words)
        //{
        //    Console.WriteLine(this.Name + words);
        //}

        public int Add(int a, int b)
        {
            return a + b;
        }

        //[Timing]
        //public void Say(string words, int age)
        //{
        //    MemberInfo method = typeof(Service).GetMethod("Say", new Type[]
        //    {
        //        typeof(string),
        //        typeof(int)
        //    });
        //            MethodAspectArgs methodAspectArgs = new MethodAspectArgs();
        //            methodAspectArgs.Argument = new object[]
        //    {
        //        words,
        //        age
        //    };
        //    Timing timing = method.GetCustomAttributes(typeof(Timing), false)[0] as Timing;
        //    timing.OnStart(methodAspectArgs);
        //    this._Say_(words, age);
        //    timing.OnEnd(methodAspectArgs);
        //}

        // Leox.AopBuildTest.Service
        //[Timing]
        //private void _Say_(string words, int age)
        //{
        //    Console.WriteLine(this.Name + words);
        //}


        [Timing]
        //[Log]
        public void Say(string words, int age)
        {
            Console.WriteLine(Name + words);
        }
    }
}
