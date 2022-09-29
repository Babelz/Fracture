using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Reflection
{
    /// <summary>
    /// Static utility class for creating dynamic property binding.
    /// </summary>
    public static class DynamicPropertyBinder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<object, object> BindSet(Type type, PropertyInfo property)
        {
            var setMethod = property.SetMethod;

            if (setMethod == null)
                throw new InvalidOperationException("property does not have set method");

            var parameterType = setMethod.GetParameters().FirstOrDefault()!.ParameterType;

            var dynamicMethod = new DynamicMethod("Set", typeof(void), new[] { typeof(object), typeof(object) }, true);
            var il            = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(type.IsClass ? OpCodes.Castclass : OpCodes.Unbox, type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Unbox_Any, parameterType);
            il.Emit(setMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setMethod);
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }

        /// <summary>
        /// Creates new dynamic set property binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<object, object> BindSet(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException($"property {propertyName} does not exist in type {type.Name}");

            return BindSet(type, property);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<object, object> BindGet(Type type, PropertyInfo property)
        {
            var getMethod = property.GetMethod;

            if (getMethod == null)
                throw new InvalidOperationException("property does not have get method");

            var dynamicMethod = new DynamicMethod("Get", typeof(object), new[] { typeof(object) }, true);
            var il            = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(type.IsClass ? OpCodes.Castclass : OpCodes.Unbox, type);
            il.Emit(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getMethod);
            il.Emit(OpCodes.Box, getMethod.ReturnType);
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        /// <summary>
        /// Creates new dynamic get property binding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<object, object> BindGet(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException($"property {propertyName} does not exist in type {type.Name}");

            return BindGet(type, property);
        }
    }
}