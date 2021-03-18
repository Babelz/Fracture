using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Reflection
{
/// <summary>
    /// Static utility class for creating dynamic bindings fro constructors.
    /// </summary>
    public static class DynamicConstructorBinder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate Bind(ConstructorInfo constructor, Type delegateType)
        {
            // Validate the delegate return type.
            var delegateMethod = delegateType.GetMethod("Invoke");
            
            if (delegateMethod == null)
                throw new InvalidOperationException("invalid delegate type");

            if (delegateMethod.ReturnType != constructor.DeclaringType)
                throw new InvalidOperationException("the return type of the delegate must match the constructors declaring type");
            
            // Validate the signatures.
            var delParams        = delegateMethod.GetParameters();
            var constructorParam = constructor.GetParameters();

            if (delParams.Length != constructorParam.Length)
                throw new InvalidOperationException("The delegate signature does not match that of the constructor");

            if (delParams.Where((t, i) => t.ParameterType != constructorParam[i].ParameterType || t.IsOut).Any())
            {
                throw new InvalidOperationException("The delegate signature does not match that of the constructor");
            }

            // Create the dynamic method.
            var method = new DynamicMethod(
                $"{constructor.DeclaringType.Name}__{Guid.NewGuid().ToString().Replace("-", "")}",
                constructor.DeclaringType,
                Array.ConvertAll(constructorParam, p => p.ParameterType),
                true);

            // Create the il.
            var gen = method.GetILGenerator();

            for (var i = 0; i < constructorParam.Length; i++)
            {
                if (i < 4)
                {
                    switch (i)
                    {
                        case 0:
                            gen.Emit(OpCodes.Ldarg_0);
                            break;
                        case 1:
                            gen.Emit(OpCodes.Ldarg_1);
                            break;
                        case 2:
                            gen.Emit(OpCodes.Ldarg_2);
                            break;
                        case 3:
                            gen.Emit(OpCodes.Ldarg_3);
                            break;
                    }
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg_S, i);
                }
            }

            gen.Emit(OpCodes.Newobj, constructor);
            gen.Emit(OpCodes.Ret);

            // Return the delegate.
            return method.CreateDelegate(delegateType);
        }
    }
}