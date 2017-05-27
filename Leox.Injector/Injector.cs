﻿
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
        public bool Inject(string path)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
            bool injected = false;

            foreach (var module in assembly.Modules)
            {
                injected = injected || CheckModule(module);
            }
            assembly.Write(path);
            return injected;
        }

        private bool CheckModule(ModuleDefinition module)
        {
            //在模块中获取所有非系统指定的类型
            var types = module.Types.Where(t => !t.IsSpecialName)
                .Where(t => !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName)).ToList();

            //获取有使用AopAtrribute的类型
            //types = types.Where(a => a.CustomAttributes.Any(b =>
            //IsSubClassOf(b.AttributeType.Resolve(), a.Module.Import(typeof(MethodAspect)).Resolve()))).ToList();
            bool injected = false;
            foreach (var type in types)
            {
                var methods = type.Methods.Where(m => !m.IsSpecialName && !m.IsSetter && !m.IsGetter).ToList();
                methods = methods.Where(m => m.CustomAttributes.Any(b =>
                          IsSubClassOf(b.AttributeType.Resolve(), m.Module.Import(typeof(MethodAspect)).Resolve()))).ToList();
                foreach (var method in methods)
                {
                    if (!methods.Any(m => m.Name == string.Format("_{0}_", method.Name)))
                    {
                        InjectMethod(method);
                        injected = true;
                    }
                }
            }
            return injected;
        }

        private void InjectMethod(MethodDefinition method)
        {
            //复制该方法
            var newMethod = CopyMethod(method);
            //清除原始方法 
            ClearOriginMethod(method);
            //重写原有方法
            OverrideOriginMethod(method, newMethod);
        }

        private void OverrideOriginMethod(MethodDefinition method, MethodDefinition newMethod)
        {
            try
            {
                var module = method.Module;
                var ilprosor = method.Body.GetILProcessor();

                //OpCodes 请参考 https://msdn.microsoft.com/zh-cn/library/system.reflection.emit.opcodes_fields(v=vs.110).aspx

                var varMemberInfo = new VariableDefinition(module.Import(typeof(System.Reflection.MemberInfo)));
                var varAspectArgs = new VariableDefinition(module.Import(typeof(MethodAspectArgs)));
                method.Body.Variables.Add(varMemberInfo);
                method.Body.Variables.Add(varAspectArgs);
                var handlerEnd = ilprosor.Create(OpCodes.Nop);

                InjectorArgs args = new InjectorArgs(method, varMemberInfo, varAspectArgs);
                args.HandlerEnd = handlerEnd;
                args.NewMethod = newMethod;

                ilprosor.Append(ilprosor.Create(OpCodes.Nop));

                // MemberInfo method = typeof(Class).GetMethod("TestMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
                InjectGetMethod(args);

                //init MethodAspectArgs
                InitMethodAspectArgs(args);

                // OnStart()
                InjectOnStart(args);

                // try{ call this.TargetMethod(...) } catch(Exception ex){  }
                TryCallTargetMethod(args);

                // OnEnd()
                InjectOnEnd(args);

                ilprosor.Append(ilprosor.Create(OpCodes.Ret));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #region Inject MemberInfo method = typeof(Class).GetMethod("TargetMethod", new Type[] { typeof(Class1), ... });

        /// <summary>
        /// 注入代码  MemberInfo method = typeof(Class).GetMethod("TargetMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
        /// </summary>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="ilprosor"></param>
        /// <param name="varTypeArr"></param>
        /// <param name="varMethod"></param>
        private void InjectGetMethod(InjectorArgs args)
        {
            var ilprosor = args.OriginMethod.Body.GetILProcessor();
            var varTypeArr = new VariableDefinition(args.Module.Import(typeof(Type[])));
            var method = args.OriginMethod;
            method.Body.Variables.Add(varTypeArr);
            // 注入typeof(Class) 并将方法字符串入栈
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Ldtoken,method.DeclaringType), // typeof(Class)
                ilprosor.Create(OpCodes.Call,method.Module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                ilprosor.Create(OpCodes.Ldstr,method.Name),
             });

            // 初始化  varTypeArr, 并将 varTypeArr 压栈
            // varTypeArr = new Type[] { typeof(Class1), typeof(Class2), ...)};
            InitTypeArr(method, method.Module, varTypeArr);

            //即实现方法 MemberInfo method = typeof(Class).GetMethod("TestMethod", new Type[] { typeof(Class1), typeof(Class2), ... });
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Call,method.Module.Import(typeof(System.Type).GetMethod("GetMethod",new Type[]{typeof(string),typeof(Type[])}))),
                ilprosor.Create(OpCodes.Stloc,args.VarMemberInfo),
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

        #endregion
       
        #region init MethodAspectArgs

        /// <summary>
        /// 初始化 OnStart  OnEnd参数 MethodAspectArgs
        /// </summary>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="varTypeArr"></param>
        private void InitMethodAspectArgs(InjectorArgs args)
        {
            var ilprosor = args.OriginMethod.Body.GetILProcessor();
            var module = args.Module;
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Nop),
                ilprosor.Create(OpCodes.Newobj,module.Import(typeof(MethodAspectArgs).GetConstructor( new Type[] {} ))), // typeof(object[])
                ilprosor.Create(OpCodes.Stloc,args.VarAspectArgs),
                ilprosor.Create(OpCodes.Ldloc,args.VarAspectArgs)
             });

            InitObjectArr(args.OriginMethod, module, ilprosor);

            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Callvirt,module.Import(typeof(MethodAspectArgs).GetMethod("set_Argument",new Type[]{ typeof(Object[]) }))),
              }
             );
        }

        private void InitObjectArr(MethodDefinition method, ModuleDefinition module, ILProcessor ilprosor)
        {
            var varArgsArr = new VariableDefinition(module.Import(typeof(Object[])));
            method.Body.Variables.Add(varArgsArr);

            //新建 Object[] varArgsArr 数组，长度为方法参数的个数
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Nop),
                ilprosor.Create(OpCodes.Ldc_I4,method.Parameters.Count),
                ilprosor.Create(OpCodes.Newarr,module.Import(typeof(System.Object))),
                ilprosor.Create(OpCodes.Stloc,varArgsArr),
                ilprosor.Create(OpCodes.Ldloc,varArgsArr)
              }
            );

            // 初始化 varArgsArr 数组
            var index = 0;
            method.Parameters.ToList().ForEach(p =>
            {
                // index 该索引是为匹配 Ldloc_S 指令将数组指定索引下的元素替换为当前返回值
                // Ldloc_S : Loads the local variable at a specific index onto the evaluation stack, short form.
                // Stelem_Ref : Replaces the array element at a given index with the object ref value (type O) on the evaluation stack.
                ilprosor.Append(ilprosor.Create(OpCodes.Ldc_I4, index++));
                ilprosor.Append(ilprosor.Create(OpCodes.Ldarg, p));
                //判断属性类型是否是值类型  如果是值类型就装箱 引用类型就强转为object
                if (p.ParameterType.IsValueType)
                {
                    ilprosor.Append(ilprosor.Create(OpCodes.Box, p.ParameterType));
                }
                else
                {
                    ilprosor.Append(ilprosor.Create(OpCodes.Castclass, module.Import(typeof(object))));
                }

                ilprosor.Append(ilprosor.Create(OpCodes.Stelem_Ref));
                ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, varArgsArr));
            });

        }

        #endregion

        #region Inject OnStart
        
        private void InjectOnStart(InjectorArgs args)
        {
            //inject attribute.OnStart();
            var method = args.OriginMethod;
            ILProcessor ilprosor = method.Body.GetILProcessor();
            var attributes = method.CustomAttributes.Where(a => IsSubClassOf(a.AttributeType.Resolve(), method.Module.Import(typeof(MethodAspect)).Resolve()));

            foreach (var attr in attributes)
            {
                var varAopAttribute = new VariableDefinition(attr.AttributeType);
                method.Body.Variables.Add(varAopAttribute);
                //TestAOPAttribute attribute = method.GetCustomAttributes(typeof(TestAOPAttribute), false)[0];
                GetAopAtrribute(args, attr, varAopAttribute);

                //call attribute.OnStart();
                Append(ilprosor, new[] 
                   {  
                       ilprosor.Create(OpCodes.Nop),  
                       ilprosor.Create(OpCodes.Ldloc, varAopAttribute ),
                        ilprosor.Create(OpCodes.Ldloc, args.VarAspectArgs),
                       ilprosor.Create(OpCodes.Callvirt,method.Module.Import(typeof(MethodAspect).GetMethod("OnStart",new Type[]{ typeof(MethodAspectArgs) }))),
                   });
                args.Attributes.Add(varAopAttribute);
            }
        }

        /// <summary>
        /// TestAOPAttribute attribute = memberinfo.GetCustomAttributes(typeof(TestAOPAttribute), false)[0]
        /// </summary>
        /// <param name="cusAttr"></param>
        /// <param name="method"></param>
        /// <param name="module"></param>
        /// <param name="ilprosor"></param>
        /// <param name="varMethod"></param>
        private void GetAopAtrribute(InjectorArgs args, CustomAttribute cusAttr, VariableDefinition varAopAttribute)
        {
            var ilprosor = args.OriginMethod.Body.GetILProcessor();
            var module = args.Module;
            Append(ilprosor, new[] 
             {  
                 ilprosor.Create(OpCodes.Nop),
                 ilprosor.Create(OpCodes.Ldloc,args.VarMemberInfo),
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

        #endregion

        #region Try Catch

        private void TryCallTargetMethod(InjectorArgs args)
        {
            var method = args.OriginMethod;
            var ilprosor = method.Body.GetILProcessor();

            var varException = new VariableDefinition(method.Module.Import(typeof(Exception)));
            method.Body.Variables.Add(varException);

            var trySatrt = ilprosor.Create(OpCodes.Nop);
            var tryEnd = ilprosor.Create(OpCodes.Stloc, varException);

            // try {
            ilprosor.Append(trySatrt);

            // this.TargetMethod(...);
            CallTargetMethod(args);

            ilprosor.Append(ilprosor.Create(OpCodes.Nop));
            ilprosor.Append(ilprosor.Create(OpCodes.Leave, args.HandlerEnd));

            // } catch(Exception ex) {
            ilprosor.Append(tryEnd);

            // methodAspectArgs.Exception = ex;
            ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, args.VarAspectArgs));
            ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, varException));
            ilprosor.Append(ilprosor.Create(OpCodes.Callvirt, method.Module.Import(typeof(MethodAspectArgs).GetMethod("set_Exception", new Type[] { typeof(Exception) }))));

            // attribute1.OnException(methodAspectArgs);
            // attribute2.OnException(methodAspectArgs); ...
            for (int index = args.Attributes.Count - 1; index >= 0; index--)
            {
                ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, args.Attributes[index]));
                ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, args.VarAspectArgs));
                ilprosor.Append(ilprosor.Create(OpCodes.Callvirt, method.Module.Import(typeof(MethodAspect).GetMethod("OnException", new Type[] { typeof(MethodAspectArgs) }))));
            }
            // }
            // catch块退出，Leave指令必不可少
            // 若程序没有执行到catch部分则可以正常运行，但是通过ILSpy查看C#代码会报错： 
            // Basci block has to end with unconditional control flow.
            ilprosor.Append(ilprosor.Create(OpCodes.Leave, args.HandlerEnd));

            ilprosor.Append(args.HandlerEnd);
            ilprosor.Append(ilprosor.Create(OpCodes.Nop));
            method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                HandlerEnd = args.HandlerEnd,
                HandlerStart = tryEnd,
                TryEnd = tryEnd,
                TryStart = trySatrt,
                CatchType = method.Module.Import(typeof(System.Exception))
            });
        }

        private void CallTargetMethod(InjectorArgs args)
        {
            var method = args.OriginMethod;
            var ilprosor = method.Body.GetILProcessor();
            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Ldarg_0), //Load this , arg0 指向的是当前对象，之后的参数指向的才是方法的入参
             });

            method.Parameters.ToList().ForEach(t => { ilprosor.Append(ilprosor.Create(OpCodes.Ldarg_S, t)); });

            Append(ilprosor, new[] 
             { 
                ilprosor.Create(OpCodes.Call,args.NewMethod),
             });
            if (method.ReturnType != method.Module.Import(typeof(void)))
            {

            }
        }


        #endregion

        private void InjectOnEnd(InjectorArgs args)
        {
            var method = args.OriginMethod;
            //call attribute.OnEnd();
            var ilprosor = method.Body.GetILProcessor();
            for (int index = args.Attributes.Count - 1; index >= 0; index--)
            {
                ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, args.Attributes[index]));
                ilprosor.Append(ilprosor.Create(OpCodes.Ldloc, args.VarAspectArgs));
                ilprosor.Append(ilprosor.Create(OpCodes.Callvirt, method.Module.Import(typeof(MethodAspect).GetMethod("OnEnd", new Type[] { typeof(MethodAspectArgs) }))));
            }
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
            //if (MethodCache.Contains(method.Name)) return MethodCache.Get(method.Name);

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
            //MethodCache.Set(method.Name, newMethod);
            return newMethod;
        }

        private bool IsSubClassOf(TypeDefinition type, TypeDefinition baseType)
        {
            if (type == null || baseType == null) return false;
            if (type.FullName == baseType.FullName) return true;

            if (type.FullName == typeof(object).FullName)
            {
                return false;
            }
            return IsSubClassOf(type.BaseType.Resolve(), baseType);
        }
    }
}
