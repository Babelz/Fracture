using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Fracture.Common.Reflection
{
    /// <summary>
    /// Static utility class containing reflection related utility methods.
    /// </summary>
    public static class ReflectionUtil
    {
        /// <summary>
        /// Creates <see cref="Delegate"/> from given method info with correct signature.
        /// </summary>
        public static Delegate CreateDelegate(MethodInfo methodInfo)
            => methodInfo.CreateDelegate(Expression.GetDelegateType(methodInfo.GetParameters().Select(p => p.ParameterType)
                                                                              .Concat(new[] { methodInfo.ReturnType })
                                                                              .ToArray()));
    }
}