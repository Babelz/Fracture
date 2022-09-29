using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Reflection
{
    /// <summary>
    /// Static utility class containing reflection related utility methods.
    /// </summary>
    public static class ReflectionUtil
    {
        /// <summary>
        /// Creates <see cref="Delegate"/> from given method info with correct signature. Underlying delegate type is selected by Expression.GetDelegateType.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDelegate(MethodInfo methodInfo)
            => methodInfo.CreateDelegate(Expression.GetDelegateType(methodInfo.GetParameters()
                                                                        .Select(p => p.ParameterType)
                                                                        .Concat(new[] { methodInfo.ReturnType })
                                                                        .ToArray()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDelegate(MethodInfo methodInfo, Type delegateType)
            => methodInfo.CreateDelegate(delegateType);

        /// <summary>
        /// Creates delegate from given method info using given delegate type.
        ///
        /// This is mainly used for crossing generic delegate boundaries by creating generic
        /// delegate of type <see cref="delegateType"/> for which the generic arguments are
        /// supplied from the given method info. If the method and the delegate have different
        /// count of arguments this method will fail.
        ///
        /// So for in example assume we have:
        ///     method info = int Add(float, double, string)
        ///     delegate type = T1 DelegateAdder&lt;T1, T2, T3&gt;(T2, T3, string)
        ///
        /// This method would create delegate of type DelegateAdder with type arguments T1 = int, T2 = float and T3 = double. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateCrossGenericDelegate(MethodInfo methodInfo, Type delegateType)
        {
            var delegateParameters   = delegateType.GetMethod("Invoke")!.GetParameters();
            var methodInfoParameters = methodInfo.GetParameters();

            if (delegateParameters.Length != methodInfoParameters.Length)
                throw new InvalidOperationException("expecting method info and delegate type to have same length parameter list")
                {
                    Data =
                    {
                        { nameof(delegateType), delegateType },
                        { "delegate parameters", delegateParameters },
                        { "method info parameters", methodInfoParameters }
                    }
                };

            var genericTypeArguments = new List<Type>();

            for (var i = 0; i < delegateParameters.Length; i++)
            {
                if (delegateParameters[i].ParameterType.ContainsGenericParameters)
                    genericTypeArguments.Add(methodInfoParameters[i].ParameterType);
            }

            if (delegateType.GetMethod("Invoke")!.ReturnType.ContainsGenericParameters)
                genericTypeArguments.Add(methodInfo.ReturnType);

            return methodInfo.CreateDelegate(delegateType.MakeGenericType(genericTypeArguments.ToArray()));
        }
    }
}