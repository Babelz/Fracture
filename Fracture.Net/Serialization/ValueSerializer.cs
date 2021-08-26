using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    }
    
    public delegate bool SupportsTypeDelegate(Type type);

    public delegate void SerializeDelegate<in T>(T value, byte[] buffer, int offset);

    public delegate T DeserializeDelegate<out T>(byte[] buffer, int offset);

    public delegate ushort GetSizeFromBufferDelegate(byte[] buffer, int offset);
        
    public delegate ushort GetSizeFromValueDelegate<in T>(T value);

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
                    // Get all static classes that have value serializer attributes. All static classes are marked as abstract and sealed at IL level.
                    types.AddRange(assembly.GetTypes()
                                           .Where(t => t.IsAbstract && 
                                                       t.IsSealed && (t.GetCustomAttribute<ValueSerializerAttribute>() != null || 
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
        private static bool IsGenericValueSerializer(Type valueSerializerType)
            => valueSerializerType.GetCustomAttribute<GenericValueSerializerAttribute>() != null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MethodInfo MakeGeneric(MethodInfo methodInfo, Type valueSerializerType, Type serializationValueType)
        {
            if (!IsGenericValueSerializer(serializationValueType))
                throw new ArgumentNullException(nameof(serializationValueType), $"value serializer {valueSerializerType.Name} is not generic, can't" +
                                                                                $"create generic method for serialization type {serializationValueType.Name}"); 
            
            return serializationValueType.IsGenericType ? methodInfo.MakeGenericMethod(serializationValueType.GetGenericArguments()) :
                                                          methodInfo.MakeGenericMethod(serializationValueType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateSerializeMethodDelegate(Type valueSerializerType, Type serializationValueType = null)
            => ReflectionUtil.CreateDelegate(GetSerializeMethodInfo(valueSerializerType, serializationValueType));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateDeserializeMethodDelegate(Type valueSerializerType, Type serializationValueType = null)
            => ReflectionUtil.CreateDelegate(GetDeserializeMethodInfo(valueSerializerType, serializationValueType));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateGetSizeFromValueMethodDelegate(Type valueSerializerType, Type serializationValueType = null)
            => ReflectionUtil.CreateDelegate(GetSizeFromValueMethodInfo(valueSerializerType, serializationValueType));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate CreateGetSizeFromBufferDelegate(Type valueSerializerType)
            => ReflectionUtil.CreateDelegate(GetSizeFromBufferMethodInfo(valueSerializerType));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSupportsTypeMethodInfo(Type valueSerializerType)
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.SupportsTypeAttribute>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {nameof(ValueSerializer.SupportsTypeAttribute)} for " +
                                                         $"value serializer", valueSerializerType);
            
            return methodInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSerializeMethodInfo(Type valueSerializerType, Type serializationValueType = null)
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.SerializeAttribute>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {nameof(ValueSerializer.SerializeAttribute)} for " +
                                                         $"value serializer", valueSerializerType);
            
            return serializationValueType == null ? methodInfo : MakeGeneric(methodInfo, valueSerializerType, serializationValueType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetDeserializeMethodInfo(Type valueSerializerType, Type serializationValueType = null)
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.DeserializeAttribute>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {nameof(ValueSerializer.DeserializeAttribute)} for " +
                                                         $"value serializer", valueSerializerType);

            return serializationValueType == null ? methodInfo : MakeGeneric(methodInfo, valueSerializerType, serializationValueType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSizeFromBufferMethodInfo(Type valueSerializerType)
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.GetSizeFromBufferAttribute>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} " +
                                                         $"for value serializer", valueSerializerType);
            
            return methodInfo;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo GetSizeFromValueMethodInfo(Type valueSerializerType, Type serializationValueType = null)
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.GetSizeFromValueAttribute>() != null);

            if (methodInfo == null) 
                throw new ValueSerializerSchemaException($"could not find static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} for " +
                                                         $"value serializer", valueSerializerType);
            
            return serializationValueType == null ? methodInfo : MakeGeneric(methodInfo, valueSerializerType, serializationValueType);
        }

        public static Type GetValueSerializerForRunType(Type serializationType)
        {
            serializationType = serializationType.IsGenericType && serializationType.GetGenericTypeDefinition() == typeof(Nullable<>) ? 
                                serializationType.GenericTypeArguments[0] : serializationType;

            foreach (var valueSerializerType in ValueSerializerTypes)
            {
                var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    .FirstOrDefault(m => m.GetCustomAttribute<ValueSerializer.SupportsTypeAttribute>() != null);
                
                if (methodInfo == null)
                    continue;
            
                if ((bool)methodInfo.Invoke(null, new object[] { serializationType }))
                    return valueSerializerType;
            }

            throw new SerializationTypeException("no value serializer type was found for serialization type", serializationType);
        }
    }
    
    /// <summary>
    /// Static utility class that provides validation rules for value serializers.
    /// </summary>
    public static class ValueSerializerValidator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateNonGenericValueSerializerSchema(Type valueSerializerType, ValueSerializerAttribute valueSerializerAttribute)
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
        private static void ValidateGenericValueSerializerSchema(Type valueSerializerType, GenericValueSerializerAttribute valueSerializerAttribute)
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
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromBufferDelegate), ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromBufferDelegate)}",
                                                         valueSerializerType);
            
            if (Delegate.CreateDelegate(typeof(GetSizeFromValueDelegate<>), ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType), false) == null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} does not match the " +
                                                         $"required method signature of {typeof(GetSizeFromValueDelegate<>)}, if the signature is " +
                                                         $"correct make sure the generic method is left open",
                                                         valueSerializerType);
        }
        
        public static void ValidateValueSerializerSchema(Type valueSerializerType)
        {
            var valueSerializerAttribute = valueSerializerType.GetCustomAttribute<ValueSerializerAttribute>();
                
            if (valueSerializerAttribute != null)
            {
                ValidateNonGenericValueSerializerSchema(valueSerializerType, valueSerializerAttribute);
                
                return;
            }
            
            var genericValueSerializerAttribute = valueSerializerType.GetCustomAttribute<GenericValueSerializerAttribute>();

            if (genericValueSerializerAttribute == null)
                throw new ValueSerializerSchemaException("type is not a valid value serializer", valueSerializerType);
            
            ValidateGenericValueSerializerSchema(valueSerializerType, genericValueSerializerAttribute);
        }
        
        public static void ValidateValueSerializerSchemas(IEnumerable<Type> valueSerializerTypes)
        {
            foreach (var valueSerializerType in valueSerializerTypes)
                ValidateValueSerializerSchema(valueSerializerType);
        }
    }
    
    /// <summary>
    /// Class that handles serialization type and run type mappings.
    /// </summary>
    public sealed class SerializationTypeRegistry
    {
        #region Fields
        private readonly Dictionary<ushort, Type> serializationTypeMapping;
        private readonly Dictionary<Type, ushort> RunTypeMapping; 
        
        // Specialization type id counter used for generating new specialization ids.
        private ushort NextSerializationTypeId;
        #endregion

        public SerializationTypeRegistry()
        {
            serializationTypeMapping = new Dictionary<ushort, Type>();
            RunTypeMapping           = new Dictionary<Type, ushort>();
        }
        
        public ushort Register(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            
            if (RunTypeMapping.ContainsKey(type)) 
                throw new SerializationTypeException("type is already specialized", type);
                
            serializationTypeMapping.Add(NextSerializationTypeId, type);
            RunTypeMapping.Add(type, NextSerializationTypeId);
            
            return NextSerializationTypeId++;
        }
        
        public bool IsRegisteredRunType(Type type)
            => RunTypeMapping.ContainsKey(type);
        
        public bool IsRegisteredSerializationType(ushort serializationTypeId)
            => serializationTypeMapping.ContainsKey(serializationTypeId);

        public ushort GetSerializationTypeId(Type type)
            => RunTypeMapping[type];
            
        public Type GetRunType(ushort serializationType)
            => serializationTypeMapping[serializationType];
    }
}