
using Leox.Aop;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Injector
{
    public class Injector
    {
        public void Inject(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);

            foreach (var module in assembly.Modules)
            {
                CheckModule(module);
            }
            assembly.Write(path);
        }

        private void CheckModule(ModuleDefinition module)
        {
            //在模块中获取所有非系统指定的类型
            var types = module.Types.Where(t => !t.IsSpecialName)
                .Where(t => !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName)).ToList();

            //获取有使用AopAtrribute的类型
            //types = types.Where(a => a.CustomAttributes.Any(b =>
            //IsSubClassOf(b.AttributeType.Resolve(), a.Module.Import(typeof(MethodAspect)).Resolve()))).ToList();

            foreach (var type in types)
            {
                var methods = type.Methods.Where(m => !m.IsSpecialName && !m.IsSetter && !m.IsGetter).ToList();
                methods = methods.Where(m => m.CustomAttributes.Any(b =>
                          IsSubClassOf(b.AttributeType.Resolve(), m.Module.Import(typeof(MethodAspect)).Resolve()))).ToList();
                foreach (var method in methods)
                {
                    InjectMethod(method);
                }
            }

        }

        private void InjectMethod(MethodDefinition method)
        {
            //复制该方法
            CopyMethod(method);
            //清除原始方法 
            ClearOriginMethod(method);
            //重写原有方法
            OverrideOriginMethod(method);
        }

        public class MethodCache
        {
            private static Dictionary<string, MethodDefinition> _cache = new Dictionary<string, MethodDefinition>(1000);

            public static void Set(string name, MethodDefinition method)
            {
                if (string.IsNullOrEmpty(name)) return;

                name = GetName(name);
                if (_cache.ContainsKey(name)) return;
                _cache[name] = method;
            }

            public static MethodDefinition Get(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                name = GetName(name);
                if (_cache.ContainsKey(name)) return _cache[name];
                return null;
            }

            public static bool Contains(string name)
            {
                name = GetName(name);
                return _cache.ContainsKey(name);
            }

            private static string GetName(string name)
            {
                return string.Format("_{0}_", name);
            }
        }

        private void OverrideOriginMethod(MethodDefinition method)
        {
            try
            {
                // 首先通过反射获取到每个指定AopAttribute的实例
                // 然后调用其OnStart方法
                var module = method.Module;
                var ilprosor = method.Body.GetILProcessor();

                //OpCodes 请参考 https://msdn.microsoft.com/zh-cn/library/system.reflection.emit.opcodes_fields(v=vs.110).aspx

                var varMethod = new VariableDefinition(module.Import(typeof(System.Reflection.MemberInfo)));
                method.Body.Variables.Add(varMethod);

                ilprosor.Append(ilprosor.Create(OpCodes.Nop));

                // MemberInfo method = typeof(Class).GetMethod("TestMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
                InjectGetMethod(method, module, varMethod);

                //此处应使用栈结构
                Stack<VariableDefinition> varStacks = new Stack<VariableDefinition>();

                InjectOnStart(method, module, varMethod, varStacks);

                // call this.TargetMethod(...)
                CallTargetMethod(method, ilprosor);

                InjectOnEnd(method, module, varStacks);

                ilprosor.Append(ilprosor.Create(OpCodes.Ret));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InjectOnEnd(MethodDefinition method, ModuleDefinition module, Stack<VariableDefinition> varStacks)
        {
            //call attribute.OnEnd();
            ILProcessor ilprosor = method.Body.GetILProcessor();
            while (varStacks.Count > 0)
            {
                var varAopAttribute = varStacks.Pop();
                Append(ilprosor, new[] 
                    {  
                        ilprosor.Create(OpCodes.Ldloc, varAopAttribute ),
                        ilprosor.Create(OpCodes.Callvirt,module.Import(typeof(MethodAspect).GetMethod("OnEnd",new Type[]{}))),
                    });
            }
        }

        private void InjectOnStart(MethodDefinition method, ModuleDefinition module, VariableDefinition varMethod, Stack<VariableDefinition> varStacks)
        {
            //inject attribute.OnStart();
            ILProcessor ilprosor = method.Body.GetILProcessor();
            var attributes = method.CustomAttributes.Where(a => IsSubClassOf(a.AttributeType.Resolve(), method.Module.Import(typeof(MethodAspect)).Resolve()));
            foreach (var attr in attributes)
            {
                var varAopAttribute = new VariableDefinition(attr.AttributeType);
                method.Body.Variables.Add(varAopAttribute);
                //TestAOPAttribute attribute = method.GetCustomAttributes(typeof(TestAOPAttribute), false)[0];
                GetAopAtrribute(attr, method, module, varMethod, varAopAttribute);

                //call attribute.OnStart();
                Append(ilprosor, new[] 
                   {  
                       ilprosor.Create(OpCodes.Nop),  
                       ilprosor.Create(OpCodes.Ldloc, varAopAttribute ),
                       ilprosor.Create(OpCodes.Callvirt,module.Import(typeof(MethodAspect).GetMethod("OnStart",new Type[]{}))),
                   });
                varStacks.Push(varAopAttribute);
            }
        }

        private void CallTargetMethod(MethodDefinition method, ILProcessor ilprosor)
        {
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Ldarg_0), //Load this , arg0 指向的是当前对象，之后的参数指向的才是方法的入参
             });

            method.Parameters.ToList().ForEach(t => { method.Body.Instructions.Add(ilprosor.Create(OpCodes.Ldarg_S, t)); });

            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Call,MethodCache.Get(method.Name)),
             });
        }

        /// <summary>
        /// TestAOPAttribute attribute = method.GetCustomAttributes(typeof(TestAOPAttribute), false)[0]
        /// </summary>
        /// <param name="cusAttr"></param>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="ilprosor"></param>
        /// <param name="varMethod"></param>
        private void GetAopAtrribute(CustomAttribute cusAttr, MethodDefinition method, ModuleDefinition module,
            VariableDefinition varMethod, VariableDefinition varAopAttribute)
        {

            var ilprosor = method.Body.GetILProcessor();

            Append(ilprosor, new[] 
             {  
                 ilprosor.Create(OpCodes.Nop),
                 ilprosor.Create(OpCodes.Ldloc,varMethod),
                 //构造GetCustomAttributes两个参数
                 ilprosor.Create(OpCodes.Ldtoken, cusAttr.AttributeType ),
                 ilprosor.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                 ilprosor.Create(OpCodes.Ldc_I4_0), // 参数 false
                 
                 ilprosor.Create(OpCodes.Callvirt,module.Import(typeof(System.Reflection.MemberInfo).GetMethod("GetCustomAttributes",new Type[]{typeof(System.Type),typeof(bool)}))),
                 ilprosor.Create(OpCodes.Ldc_I4_0),
                 // Ldelem_Ref : 获取数组返回的第Index个元素
                 // 更多解释及指令操作请自行查询msdn :  https://social.msdn.microsoft.com/Search/zh-CN?query=Ldelem_Ref&pgArea=header&emptyWatermark=true&ac=4
                 ilprosor.Create(OpCodes.Ldelem_Ref),
                 // Isinst 指令执行顺序
                 //1. An object reference is pushed onto the stack.
                 //2. The object reference is popped from the stack and tested to see if it is an instance of the class passed in class.
                 //3. The result (either an object reference or a null reference) is pushed onto the stack.
                 ilprosor.Create(OpCodes.Isinst,cusAttr.AttributeType),
                 //ilprosor.Create(OpCodes.Ldc_I4,index),
                 ilprosor.Create(OpCodes.Stloc,varAopAttribute),
                 ilprosor.Create(OpCodes.Nop),
            });
        }

        /// <summary>
        /// 注入代码  MemberInfo method = typeof(Class).GetMethod("TargetMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
        /// </summary>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="ilprosor"></param>
        /// <param name="varTypeArr"></param>
        /// <param name="varMethod"></param>
        private void InjectGetMethod(MethodDefinition method, ModuleDefinition module, VariableDefinition varMethod)
        {
            var ilprosor = method.Body.GetILProcessor();
            var varTypeArr = new VariableDefinition(module.Import(typeof(Type[])));
            method.Body.Variables.Add(varTypeArr);
            //将方法字符串入栈
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Ldtoken,method.DeclaringType),
                ilprosor.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                ilprosor.Create(OpCodes.Ldstr,method.Name),
             });

            // 初始化  varTypeArr, 并将 varTypeArr 压栈
            // varTypeArr = new Type[] { typeof(Class1), typeof(Class2), ...)};
            InitTypeArr(method, module, varTypeArr);

            //即实现方法 MemberInfo method = typeof(Class).GetMethod("TestMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetMethod",new Type[]{typeof(string),typeof(Type[])}))),
                ilprosor.Create(OpCodes.Stloc,varMethod),
             });
        }

        /// <summary>
        /// 初始化Type[] 数组
        /// </summary>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="varTypeArr"></param>
        private void InitTypeArr(MethodDefinition method, ModuleDefinition module, VariableDefinition varTypeArr)
        {
            var ilprosor = method.Body.GetILProcessor();
            //新建 Type[] varTypeArr 数组，长度为方法参数的个数
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Ldc_I4,method.Parameters.Count),
                ilprosor.Create(OpCodes.Newarr,module.Import(typeof(System.Type))),
                ilprosor.Create(OpCodes.Stloc,varTypeArr),
                ilprosor.Create(OpCodes.Ldloc,varTypeArr)
              }
            );

            // 初始化 varTypeArr 数组
            var index = 0;
            method.Parameters.ToList().ForEach(p =>
            {
                Append(ilprosor, new[] 
                 { 
                     // index 该索引是为匹配 Ldloc_S 指令将数组指定索引下的元素替换为当前返回值
                     // Ldloc_S : Loads the local variable at a specific index onto the evaluation stack, short form.
                     // Stelem_Ref : Replaces the array element at a given index with the object ref value (type O) on the evaluation stack.
                     ilprosor.Create(OpCodes.Ldc_I4,index++ ),         
                     ilprosor.Create(OpCodes.Ldtoken,p.ParameterType),  // 获取参数类型Type，即typeof()的实现
                     ilprosor.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     ilprosor.Create(OpCodes.Stelem_Ref),
                     ilprosor.Create(OpCodes.Ldloc,varTypeArr), //此处需要再次将varTypeArr用作操作数，varTypeArr[index] = typeof(x)
                 });
            });
        }

        private void Append(ILProcessor ilprosor, Instruction[] ins)
        {
            if (ins != null)
                Array.ForEach(ins, item => { ilprosor.Append(item); });
        }

        /// <summary>
        /// 清除原始方法
        /// </summary>
        /// <param name="method"></param>
        private void ClearOriginMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.Variables.Clear();
            //method.Body.Instructions.Add(il.Create(OpCodes.Nop));
        }

        private static MethodDefinition CopyMethod(MethodDefinition method)
        {
            if (MethodCache.Contains(method.Name)) return MethodCache.Get(method.Name);

            string methodName = string.Format("_{0}_", method.Name);
            MethodDefinition newMethod = new MethodDefinition(methodName, method.Attributes, method.ReturnType);
            newMethod.IsPrivate = true;
            newMethod.IsStatic = method.IsStatic;

            method.CustomAttributes.ToList().ForEach(t => { newMethod.CustomAttributes.Add(t); });
            method.Body.Instructions.ToList().ForEach(t => { newMethod.Body.Instructions.Add(t); });
            method.Body.Variables.ToList().ForEach(t => { newMethod.Body.Variables.Add(t); });
            method.Body.ExceptionHandlers.ToList().ForEach(t => { newMethod.Body.ExceptionHandlers.Add(t); });
            method.Parameters.ToList().ForEach(t => { newMethod.Parameters.Add(t); });
            method.GenericParameters.ToList().ForEach(t => { newMethod.GenericParameters.Add(t); });

            newMethod.Body.LocalVarToken = method.Body.LocalVarToken;
            newMethod.Body.InitLocals = method.Body.InitLocals;

            method.DeclaringType.Methods.Add(newMethod);
            MethodCache.Set(method.Name, newMethod);
            return newMethod;
        }

        private bool IsSubClassOf(TypeDefinition type, TypeDefinition baseType)
        {
            if (type == null || baseType == null) return false;
            if (type.GetType() == baseType.GetType()) return true;

            if (type.FullName == typeof(object).FullName)
            {
                return false;
            }
            return IsSubClassOf(type.BaseType.Resolve(), baseType);
        }
    }
}
