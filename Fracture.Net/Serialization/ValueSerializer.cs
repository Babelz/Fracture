using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Common.Reflection;
using Serilog;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Attribute for annotating classes that provide non-generic value serialization for specific type. For example primitive types (int, float etc) are good
    /// candidates for this level of serialization as their types can be inferred at runtime easily without introducing any boxing. Non-generic value serializers
    /// should always resolve the serialization type to specific run time type.
    /// </summary>
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

    /// <summary>
    /// Attribute annotating classes that provide generic value serialization for subset of types. Examples for generic serialization types are enums and classes
    /// where the serialization type is know only at run time and the type must be inferred during runtime from the object being serialized or deserialized.
    /// Generic value serializers should resolve the serialization type based on value schematics and type information provided by generics at run time. 
    /// </summary>
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
        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="SupportsTypeDelegate"/> in value serializers. This attribute is for
        /// all value serializer types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SupportsTypeAttribute : Attribute
        {
            public SupportsTypeAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="SerializeDelegate{T}"/> in value serializers. This attribute is for
        /// all value serializer types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SerializeAttribute : Attribute
        {
            public SerializeAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="DeserializeDelegate{T}"/> in value serializers. This attribute is for
        /// all value serializer types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class DeserializeAttribute : Attribute
        {
            public DeserializeAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="GetSizeFromValueDelegate{T}"/> or in non-generic context for
        /// <see cref="GetSizeFromValueDelegate"/> in value serializers. This attribute is for all value serializer types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class GetSizeFromBufferAttribute : Attribute
        {
            public GetSizeFromBufferAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="GetSizeFromValueDelegate{T}"/> in value serializers. This attribute is for
        /// all value serializer types.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class GetSizeFromValueAttribute : Attribute
        {
            public GetSizeFromValueAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="CanExtendTypeDelegate"/> in value serializers. This attribute is required
        /// for serializers that are annotated with <see cref="ExtendableValueSerializerAttribute"/>.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class CanExtendTypeAttribute : Attribute
        {
            public CanExtendTypeAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute used to annotate methods that provide interface for <see cref="ExtendTypeDelegate"/> in value serializers. This attribute is required
        /// for serializers that are annotated with <see cref="ExtendableValueSerializerAttribute"/>.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class ExtendTypeAttribute : Attribute
        {
            public ExtendTypeAttribute()
            {
            }
        }
    }

    /// <summary>
    /// Returns boolean declaring whether given serialization type is supported by the value serializer.
    /// </summary>
    public delegate bool SupportsTypeDelegate(Type type);

    /// <summary>
    /// Serializes given object of specific type to given buffer beginning at given offset.
    /// </summary>
    public delegate void SerializeDelegate<in T>(T value, byte[] buffer, int offset);

    /// <summary>
    /// Deserializes object of specified type from given buffer beginning at given offset.
    /// </summary>
    public delegate T DeserializeDelegate<out T>(byte[] buffer, int offset);

    /// <summary>
    /// Returns the size of an object inside the buffer beginning at given offset.
    /// </summary>
    public delegate ushort GetSizeFromBufferDelegate(byte[] buffer, int offset);

    /// <summary>
    /// Returns the size of an object inside the buffer using generic type information provided beginning at given offset. 
    /// </summary>
    public delegate ushort GetSizeFromBufferDelegate<in T>(byte[] buffer, int offset);

    /// <summary>
    /// Returns size of the value when it is serialized.
    /// </summary>
    public delegate ushort GetSizeFromValueDelegate<in T>(T value);

    /// <summary>
    /// Returns boolean declaring whether this serialization type can be extended by the value serializer.
    /// </summary>
    public delegate bool CanExtendTypeDelegate(Type type);

    /// <summary>
    /// Extends given serialization type inside the serializer with special serialization instructions. For example the type might require some generic
    /// reducing before it can be serialized.
    /// </summary>
    public delegate void ExtendTypeDelegate(Type type);

    /// <summary>
    /// Static class that provides interface for working with value serializers indirectly. 
    /// </summary>
    public static class ValueSerializerRegistry
    {
        #region Static fields
        private static readonly List<Type> ValueSerializerTypes = new List<Type>();
        #endregion

        static ValueSerializerRegistry()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    ValueSerializerTypes.AddRange(assembly.GetTypes()
                                                      .Where(t => (t.GetCustomAttribute<ValueSerializerAttribute>() != null ||
                                                                   t.GetCustomAttribute<GenericValueSerializerAttribute>() != null)));
                }
                catch (ReflectionTypeLoadException e)
                {
                    Log.Warning(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            }
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
            return serializationValueType.IsGenericType
                ? methodInfo.MakeGenericMethod(serializationValueType.GetGenericArguments())
                : methodInfo.MakeGenericMethod(serializationValueType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MethodInfo GetValuerSerializerMethodInfo<T>(Type valueSerializerType, Type serializationValueType = null) where T : Attribute
        {
            var methodInfo = valueSerializerType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m => m.GetCustomAttribute<T>() != null);

            if (methodInfo == null)
                throw new ValueSerializerSchemaException($"could not find static method annotated with {typeof(T).Name} for " +
                                                         $"value serializer",
                                                         valueSerializerType);

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
            => ReflectionUtil.CreateCrossGenericDelegate(GetSizeFromValueMethodInfo(valueSerializerType, serializationValueType),
                                                         typeof(GetSizeFromValueDelegate<>));

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
        public static SupportsTypeDelegate CreateSupportsTypeDelegate(Type valueSerializerType)
            => (SupportsTypeDelegate)ReflectionUtil.CreateDelegate(GetSupportsTypeMethodInfo(valueSerializerType), typeof(SupportsTypeDelegate));

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
                if (CreateSupportsTypeDelegate(valueSerializerType)(serializationType))
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

            if (Delegate.CreateDelegate(typeof(SerializeDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType),
                                        ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType),
                                        false) ==
                null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.SerializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(SerializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);

            if (Delegate.CreateDelegate(typeof(DeserializeDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType),
                                        ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType),
                                        false) ==
                null)
                throw new ValueSerializerSchemaException($"static method annotated with {nameof(ValueSerializer.DeserializeAttribute)} does not match the " +
                                                         $"required method signature of {typeof(DeserializeDelegate<>)}, if the signature is correct" +
                                                         $"make sure the generic method is closed and the type parameter is correct",
                                                         valueSerializerType);

            if (Delegate.CreateDelegate(typeof(GetSizeFromBufferDelegate), ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType), false) ==
                null)
                throw new ValueSerializerSchemaException(
                    $"static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} does not match the " +
                    $"required method signature of {typeof(GetSizeFromBufferDelegate)}",
                    valueSerializerType);

            if (Delegate.CreateDelegate(typeof(GetSizeFromValueDelegate<>).MakeGenericType(valueSerializerAttribute.SerializationType),
                                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType),
                                        false) ==
                null)
                throw new ValueSerializerSchemaException(
                    $"static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} does not match the " +
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

            if (Delegate.CreateDelegate(typeof(GetSizeFromBufferDelegate<>), ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType), false) ==
                null)
                throw new ValueSerializerSchemaException(
                    $"static method annotated with {nameof(ValueSerializer.GetSizeFromBufferAttribute)} does not match the " +
                    $"required method signature of {typeof(GetSizeFromBufferDelegate<>)}",
                    valueSerializerType);

            if (Delegate.CreateDelegate(typeof(GetSizeFromValueDelegate<>), ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType), false) ==
                null)
                throw new ValueSerializerSchemaException(
                    $"static method annotated with {nameof(ValueSerializer.GetSizeFromValueAttribute)} does not match the " +
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