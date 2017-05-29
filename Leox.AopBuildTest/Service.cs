using Leox.Aop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Leox.AopBuildTest
{
    public class Service
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
        private void _Say_(string words, int age)
        {
            Console.WriteLine(this.Name + words);
            throw new Exception("stack over flow.");
        }


       [Timing(Order=12)]
       [Log(Order = 1, ExceptionStrategy = ExceptionStrategy.UnThrow)]
       public int Say(string words, int age)
       {
           Console.WriteLine(Name + words);
           //throw new Exception("stack over flow.");
           return 12345;
       }

        // Leox.AopBuildTest.Service
        // Leox.AopBuildTest.Service
        //[Timing]
        // Leox.AopBuildTest.Service
        //[Log(Order = 1), Timing(Order = 12)]
        public void Say12(string words, int age)
        {
            MemberInfo method = typeof(Service).GetMethod("Say", new Type[]
	        {
		        typeof(string),
		        typeof(int)
	        });
            MethodAspectArgs methodAspectArgs = new MethodAspectArgs();
            methodAspectArgs.Argument = new object[]
	        {
		        (object)words,
		        age
	        };
            MAList mAList = new MAList();
            Log item = method.GetCustomAttributes(typeof(Log), false)[0] as Log;
            mAList.Add(item);
            Timing item2 = method.GetCustomAttributes(typeof(Timing), false)[0] as Timing;
            mAList.Add(item2);
            foreach (MethodAspect current in mAList)
            {
                current.OnStart(methodAspectArgs);
            }
            try
            {
                this._Say_(words, age);
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


    }
}
