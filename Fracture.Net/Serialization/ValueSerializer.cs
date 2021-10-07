using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Common.Reflection;
using NLog;

namespace Fracture.Net.Serialization
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ValueSerializerAttribute : Attribute
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }
        #endregion

        public ValueSerializerAttribute(Type serializationType)
        {
            SerializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GenericValueSerializerAttribute : Attribute
    {
        public GenericValueSerializerAttribute()
        {
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExtendableValueSerializerAttribute : Attribute
    {
        public ExtendableValueSerializerAttribute()
        {
        }
    }
    
    public static class ValueSerializer
    {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SupportsTypeAttribute : Attribute 
        {
            public SupportsTypeAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SerializeAttribute : Attribute 
        {
            public SerializeAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class DeserializeAttribute : Attribute 
        {
            public DeserializeAttribute()
            {
            }
        }
        

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class GetSizeFromBufferAttribute : Attribute 
        {
            public GetSizeFromBufferAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class GetSizeFromValueAttribute : Attribute 
        {
            public GetSizeFromValueAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class CanExtendTypeAttribute : Attribute
        {
            public CanExtendTypeAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class ExtendTypeAttribute : Attribute
        {
            public ExtendTypeAttribute()
            {
            }
        }
    }
    
    public delegate bool SupportsTypeDelegate(Type type);

    public delegate void SerializeDelegate<in T>(T value, byte[] buffer, int offset);

    public delegate T DeserializeDelegate<out T>(byte[] buffer, int offset);

    public delegate ushort GetSizeFromBufferDelegate(byte[] buffer, int offset);

    public delegate ushort GetSizeFromBufferDelegate<in T>(byte[] buffer, int offset);

    public delegate ushort GetSizeFromValueDelegate<in T>(T value);
    
    public delegate bool CanExtendTypeDelegate(Type type);
    
    public delegate void ExtendTypeDelegate(Type type);
    
    /// <summary>
    /// Static class that provides interface for working with value serializers indirectly. 
    /// </summary>
    public static class ValueSerializerRegistry
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Static properties
        public static IEnumerable<Type> ValueSerializerTypes
        {
            get;
        }
        #endregion
        
        static ValueSerializerRegistry()
        {
            var types = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                try
                {
                    types.AddRange(assembly.GetTypes().Where(t => (t.GetCustomAttribute<ValueSerializerAttribute>() != null || 
                                                                   t.GetCustomAttribute<GenericValueSerializerAttribute>() != null)));
                }   
                catch (ReflectionTypeLoadException e)
                {
                    Log.Warn(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            }
            
            ValueSerializerTypes = types;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGenericValueSerializer(MemberInfo valueSerializerType)
            => valueSerializerType.GetCustomAttribute<GenericValueSerializerAttribute>() != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExtendableValueSerializer(MemberInfo valueSerializerType)
            => valueSerializerType.GetCustomAttribute<ExtendableValueSerializerAttribute>() != null;
        
        /// <summary>
        /// Specializes given method info based on the value serializer type.    
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MethodInfo SpecializeMethodInfo(MethodInfo methodInfo, Type valueSerializerType, Type serializationValueType)
        {
            // Can't specialize non-generic methods.
            if (!methodInfo.IsGenericMethod)
                return methodInfo;
            
            // No serialization value type hint was provided so assuming the method should be good to go as it is for delegate creation.
            if (serializationValueType == null)
                return methodInfo;
            
            // Nothing to specialize, just return the method info.
            if (!IsGenericValueSerializer(valueSerializerType))
                return methodInfo;

            // Handle special case with arrays as they are not handled as generic types.
            if (serializationValueType.IsArray)
                return methodInfo.MakeGenericMethod(serializationValueType.GetElementType());
            
            // Make method info generic based on serialization type.
            return serializationValueType.IsGenericType ? methodInfo.MakeGenericMethod(serializationValueType.GetGenericArguments()) : 
                                                          methodInfo.MakeGenericMethod(serializationValueType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MethodInfo GetValuerSerializerMethodInfo<T>(Type valueSerializerType, Type serializationValueType = null) where T : Attribute
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<T>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {typeof(T).Name} for " +
                                                         $"value serializer", valueSerializerType);
            
            return serializationValueType == null ? methodInfo : SpecializeMethodInfo(methodInfo, valueSerializerType, serializationValueType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateSerializeDelegate(Type valueSerializerType, Type serializationValueType)
            => ReflectionUtil.CreateCrossGenericDelegate(GetSerializeMethodInfo(valueSerializerType, serializationValueType), typeof(SerializeDelegate<>));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDeserializeDelegate(Type valueSerializerType, Type serializationValueType)
            => ReflectionUtil.CreateCrossGenericDelegate(GetDeserializeMethodInfo(valueSerializerType, serializationValueType), typeof(DeserializeDelegate<>));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateGetSizeFromValueDelegate(Type valueSerializerType, Type serializationValueType)
            => ReflectionUtil.CreateCrossGenericDelegate(GetSizeFromValueMethodInfo(valueSerializerType, serializationValueType), typeof(GetSizeFromValueDelegate<>));
           
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateGetSizeFromBufferDelegate(Type valueSerializerType, Type serializationValueType)
            => ReflectionUtil.CreateDelegate(GetSizeFromBufferMethodInfo(valueSerializerType, serializationValueType), typeof(GetSizeFromBufferDelegate));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CanExtendTypeDelegate CreateCanExtendTypeDelegate(Type valueSerializerType)
            => (CanExtendTypeDelegate)ReflectionUtil.CreateDelegate(GetCanExtendTypeMethodInfo(valueSerializerType), typeof(CanExtendTypeDelegate));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExtendTypeDelegate CreateExtendTypeDelegate(Type valueSerializerType)
            => (ExtendTypeDelegate)ReflectionUtil.CreateDelegate(GetExtendTypeMethodInfo(valueSerializerType), typeof(ExtendTypeDelegate));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSupportsTypeMethodInfo(Type valueSerializerType)
            => GetValuerSerializerMethodInfo<ValueSerializer.SupportsTypeAttribute>(valueSerializerType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSerializeMethodInfo(Type valueSerializerType, Type serializationValueType = null)
            => GetValuerSerializerMethodInfo<ValueSerializer.SerializeAttribute>(valueSerializerType, serializationValueType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetDeserializeMethodInfo(Type valueSerializerType, Type serializationValueType = null)
            => GetValuerSerializerMethodInfo<ValueSerializer.DeserializeAttribute>(valueSerializerType, serializationValueType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSizeFromValueMethodInfo(Type valueSerializerType, Type serializationValueType = null)
            => GetValuerSerializerMethodInfo<ValueSerializer.GetSizeFromValueAttribute>(valueSerializerType, serializationValueType);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSizeFromBufferMethodInfo(Type valueSerializerType, Type serializationValueType = null)
            => GetValuerSerializerMethodInfo<ValueSerializer.GetSizeFromBufferAttribute>(valueSerializerType, serializationValueType);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetCanExtendTypeMethodInfo(Type valueSerializerType)
            => GetValuerSerializerMethodInfo<ValueSerializer.CanExtendTypeAttribute>(valueSerializerType);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetExtendTypeMethodInfo(Type valueSerializerType)
            => GetValuerSerializerMethodInfo<ValueSerializer.ExtendTypeAttribute>(valueSerializerType);

        public static Type GetValueSerializerForRunType(Type serializationType)
        {
            foreach (var valueSerializerType in ValueSerializerTypes)
            {
                var supportsTypeMethodInfo = GetSupportsTypeMethodInfo(valueSerializerType);
                
                if (supportsTypeMethodInfo == null)
                    continue;
            
                if ((bool)supportsTypeMethodInfo.Invoke(null, new object[] { serializationType }))
                    return valueSerializerType;
            }

            throw new SerializationTypeException("no value serializer type was found for serialization type", serializationType);
        }
    }
    
    /// <summary>
    /// Static utility class that provides validation rules for value serializers.
    /// </summary>
    public static class ValueSerializerSchemaValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateNonGenericSchema(Type valueSerializerType, ValueSerializerAttribute valueSerializerAttribute)
        {            
            if (Delegate.CreateDelegate(typeof(SupportsTypeDelegate), ValueSerializerRegistry.GetSupportsTypeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.SupportsTypeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(SupportsTypeDelegate)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(SerializeDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType), ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.SerializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(SerializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(DeserializeDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType), ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.DeserializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(DeserializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromBufferDelegate), ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromBufferDelegate)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromValueDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType), ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromValueDelegate<>)}, if the signature is " +
                                                         $"correct make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateGenericSchema(Type valueSerializerType)
        {
            if (Delegate.CreateDelegate(typeof(SupportsTypeDelegate), ValueSerializerRegistry.GetSupportsTypeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.SupportsTypeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(SupportsTypeDelegate)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(SerializeDelegate<>), ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.SerializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(SerializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is left open",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(DeserializeDelegate<>), ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.DeserializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(DeserializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is left open",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromBufferDelegate<>), ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromBufferDelegate<>)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromValueDelegate<>), ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromValueDelegate<>)}, if the signature is " +
                                                         $"correct make sure the generic method is left open",
                                                         valueSerializerType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateExtendableSchema(Type valueSerializerType)
        {
            if (Delegate.CreateDelegate(typeof(CanExtendTypeDelegate), ValueSerializerRegistry.GetCanExtendTypeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.CanExtendTypeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(CanExtendTypeDelegate)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(ExtendTypeDelegate), ValueSerializerRegistry.GetExtendTypeMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.ExtendTypeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(ExtendTypeDelegate)}, if the signature is correct" +
                                                         $"make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);
        }
        
        public static void ValidateSchema(Type valueSerializerType)
        {
            var valueSerializerAttribute           = valueSerializerType.GetCustomAttribute<ValueSerializerAttribute>();
            var genericValueSerializerAttribute    = valueSerializerType.GetCustomAttribute<GenericValueSerializerAttribute>();
            var extendableValueSerializerAttribute = valueSerializerType.GetCustomAttribute<ExtendableValueSerializerAttribute>();

            if (valueSerializerAttribute == null && genericValueSerializerAttribute == null)
                throw new ValueSerializerSchemaException("type has no value serializer attribute", valueSerializerType);

            if (!valueSerializerType.IsAbstract && !valueSerializerType.IsSealed)
                throw new ValueSerializerSchemaException("value serializers are expected to be static classes", valueSerializerType);
            
            if (valueSerializerAttribute != null)
                ValidateNonGenericSchema(valueSerializerType, valueSerializerAttribute);
            else
                ValidateGenericSchema(valueSerializerType);
            
            if (extendableValueSerializerAttribute != null)
                ValidateExtendableSchema(valueSerializerType);
        }
        
        public static void ValidateSchemas(IEnumerable<Type> valueSerializerTypes)
        {
            foreach (var valueSerializerType in valueSerializerTypes)
                ValidateSchema(valueSerializerType);
        }
    }
}