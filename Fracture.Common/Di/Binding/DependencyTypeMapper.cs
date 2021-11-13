using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Di.Binding
{
    public static class DependencyTypeMapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorInfo GetBindingConstructor(Type type)
            => type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .First(c => c.GetCustomAttribute<BindingConstructorAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<MethodInfo> GetBindingMethods(Type type)
            => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Where(c => c.GetCustomAttribute<BindingMethodAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<PropertyInfo> GetBindingProperties(Type type)
            => type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Where(c => c.GetCustomAttribute<BindingPropertyAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasDefaultConstructor(Type type)
            => type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Any(c => c.GetParameters().Length == 0);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasBindingConstructor(Type type)
            => type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Any(c => c.GetCustomAttribute<BindingConstructorAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasBindingMethods(Type type)
            => type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Any(c => c.GetCustomAttribute<BindingMethodAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasBindingProperties(Type type)
            => type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                   .Any(c => c.GetCustomAttribute<BindingPropertyAttribute>() != null);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type[] Map(Type type, DependencyBindingOptions options)
        {
            var types = new List<Type>();

            if ((options & DependencyBindingOptions.Classes) == DependencyBindingOptions.Classes && type.IsClass)
            {
                var it = type;

                while (it != null && it != typeof(object))
                {
                    types.Add(it);

                    it = it.BaseType;
                }
            }
            else if ((options & DependencyBindingOptions.Class) == DependencyBindingOptions.Class && type.IsClass)
            {
                types.Add(type);
            }
            else if ((options & DependencyBindingOptions.AbstractClasses) == DependencyBindingOptions.AbstractClasses)
            {
                var it = type;

                while (it != null && it != typeof(object))
                {
                    if (it.IsAbstract)
                        types.Add(it);

                    it = it.BaseType;
                }
            }

            if ((options & DependencyBindingOptions.Interfaces) == DependencyBindingOptions.Interfaces)
            {
                if (type.IsInterface) 
                    types.Add(type);    
                
                types.AddRange(type.GetInterfaces());
            }
            
            return types.ToArray();
        }
    }
}
