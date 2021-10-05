using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Fracture.Common.Reflection
{
    /// <summary>
    /// Utility class for generating dynamic methods.
    /// </summary>
    public class DynamicMethodBuilder
    {
        #region Fields
        private readonly DynamicMethod dynamicMethod;
        
        private readonly ILGenerator il;
        #endregion
        
        public DynamicMethodBuilder(string name, Type returnType, Type[] parameterTypes)
        {
            dynamicMethod = new DynamicMethod(name, returnType, parameterTypes, true);

            il = dynamicMethod.GetILGenerator();
        }

        public LocalBuilder DeclareLocal(Type type)
        {
            return il.DeclareLocal(type);    
        }
        
        public void Emit(OpCode op)
        {
            il.Emit(op);
        }

        public void Emit(OpCode op, Label label)
        {
            il.Emit(op, label);
        }
        
        public void Emit(OpCode op, int value)
        {
            il.Emit(op, value);
        }
        
        public void Emit(OpCode op, string value)
        {
            il.Emit(op, value);
        }

        public void Emit(OpCode op, Type type)
        {
            il.Emit(op, type);
        }
        
        public void Emit(OpCode op, LocalBuilder local)
        {
            il.Emit(op, local);
        }
        
        public void Emit(OpCode op, FieldInfo field)
        {
            il.Emit(op, field);
        }
        
        public void Emit(OpCode op, MethodInfo method)
        {
            il.Emit(op, method);
        }

        public void Emit(OpCode op, ConstructorInfo constructor)
        {
            il.Emit(op, constructor);
        }
        
        public Label DefineLabel()
        {
            return il.DefineLabel();
        }
        
        public void MarkLabel(Label label)
        {
            il.MarkLabel(label);
        }
        
        public Delegate CreateDelegate(Type type)
            => dynamicMethod.CreateDelegate(type);
    }
}