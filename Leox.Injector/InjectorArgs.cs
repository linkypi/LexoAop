using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leox.Injector
{
    internal sealed class InjectorArgs
    {
        public MethodDefinition OriginMethod { get; set; }
        public MethodDefinition NewMethod { get; set; }
        public VariableDefinition VarAttrList { get; set; }
        public VariableDefinition VarItem { get; set; }
        public VariableDefinition VarHasNext { get; set; }
        /// <summary>
        /// 使用foreach遍历集合时需要用到 Enumerator
        /// </summary>
        public VariableDefinition VarEnumerator { get; set; }

        public ModuleDefinition Module
        {
            get
            {
                if (OriginMethod != null) return OriginMethod.Module;
                return null;
            }
        }

        private ILProcessor _iLProcessor;
        public ILProcessor ILProcessor
        {
            get
            {
                if (_iLProcessor == null) {
                    _iLProcessor = OriginMethod.Body.GetILProcessor();
                }
                return _iLProcessor;
            }
        }

        /// <summary>
        /// attribute
        /// </summary>
        public VariableDefinition VarMemberInfo { get; set; }
        /// <summary>
        /// MethodAspectArgs
        /// </summary>
        public VariableDefinition VarAspectArgs { get; set; }
        public Instruction HandlerEnd { get; set; }

        public VariableDefinition ReturnValue { get; set; }
        public VariableDefinition VarException { get; set; }
        public InjectorArgs() { }
        public InjectorArgs(MethodDefinition method, VariableDefinition varMemberInfo, VariableDefinition varAspectArgs)
        {
            OriginMethod = method;
            VarMemberInfo = varMemberInfo;
            VarAspectArgs = varAspectArgs;
        }

    }
}
