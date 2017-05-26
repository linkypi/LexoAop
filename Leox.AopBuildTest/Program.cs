using Leox.Aop;
using System;
using System.Collections.Generic;
using System.Linq;
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
            service.Say(" say hello.");
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
        public override void OnStart()
        {
            Console.WriteLine("timing start");
        }

        public override void OnEnd()
        {
            Console.WriteLine("timing end");
        }
    }
    class Log : MethodAspect
    {
        public override void OnStart()
        {
            Console.WriteLine("log start");
        }

        public override void OnEnd()
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

        [Timing]
        [Log]
        public void Say(string words)
        {
            Console.WriteLine(Name + words);
        }
    }
}
