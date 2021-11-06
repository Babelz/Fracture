using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Serialization
{
    public static class NullableReducer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Delegate CreateNullableSerializeDelegate<T>(Delegate serializationDelegate) where T : struct
        {            
            SerializeDelegate<T?> CreateDelegate()
            {
                var serializeDelegate = (SerializeDelegate<T>)serializationDelegate;
                
                void Serialize(T? value, byte[] buffer, int offset)
                {
                    if (!value.HasValue) return;
                    
                    serializeDelegate(value.Value, buffer, offset);
                }
                
                return Serialize;
            }
            
            return CreateDelegate();
        }
         
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Delegate CreateNullableDeserializeDelegate<T>(Delegate serializationDelegate) where T : struct
        {            
            DeserializeDelegate<T?> CreateDelegate()
            {                
                var deserializeDelegate = (DeserializeDelegate<T>)serializationDelegate;

                T? Deserialize(byte[] buffer, int offset)
                    => deserializeDelegate(buffer, offset);
                
                return Deserialize;
            }
            
            return CreateDelegate();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Delegate CreateNullableGetSizeFromValueDelegate<T>(Delegate serializationDelegate) where T : struct
        {
            GetSizeFromValueDelegate<T?> CreateDelegate()
            {              
                var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)serializationDelegate;

                ushort GetSizeFromValue(T? value)
                    => value.HasValue ? getSizeFromValueDelegate(value.Value) : (ushort) 0;
                
                return GetSizeFromValue;
            }
            
            return CreateDelegate();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Delegate UnwindNullableSerializationDelegate(MethodInfo createDelegateMethod, Delegate serializationDelegate, Type nullableType)
            => (Delegate)createDelegateMethod.MakeGenericMethod(nullableType.GetGenericArguments()[0]).Invoke(null, new object[] { serializationDelegate });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertNullableTypeParameter(Type nullableType)
        {
            if (!nullableType.IsGenericType || nullableType.GetGenericTypeDefinition() != typeof(Nullable<>))
                throw new InvalidEnumArgumentException($"expecting {nameof(Nullable)} type");
            
            if (nullableType.GetGenericArguments().Length == 0)
                throw new InvalidEnumArgumentException($"expecting {nameof(Nullable)} type with generic arguments");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate UnwindSerializeDelegate(Delegate serializeDelegate, Type nullableType)
        {
            AssertNullableTypeParameter(nullableType);
            
            return UnwindNullableSerializationDelegate(
                typeof(NullableReducer).GetMethod(nameof(CreateNullableSerializeDelegate), BindingFlags.Static | BindingFlags.NonPublic), 
                serializeDelegate, 
                nullableType
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate UnwindDeserializeDelegate(Delegate deserializeDelegate, Type nullableType)
        {
            AssertNullableTypeParameter(nullableType);
            
            return UnwindNullableSerializationDelegate(
                typeof(NullableReducer).GetMethod(nameof(CreateNullableDeserializeDelegate), BindingFlags.Static | BindingFlags.NonPublic), 
                deserializeDelegate, 
                nullableType
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate UnwindGetSizeFromValueDelegate(Delegate getSizeFromValueDelegate, Type nullableType)
        {
            AssertNullableTypeParameter(nullableType);
            
            return UnwindNullableSerializationDelegate(
                typeof(NullableReducer).GetMethod(nameof(CreateNullableGetSizeFromValueDelegate), BindingFlags.Static | BindingFlags.NonPublic), 
                getSizeFromValueDelegate, 
                nullableType
            );
        }
    }
    
    [GenericValueSerializer]
    [ExtendableValueSerializer]
    public class NullableSerializer
    {        
        #region Static fields
        private static readonly Dictionary<Type, Delegate> SerializeDelegates         = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> DeserializeDelegates       = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromBufferDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromValueDelegates  = new Dictionary<Type, Delegate>();
        #endregion
                
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        [ValueSerializer.CanExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanExtendType(Type type)
            => SupportsType(type) && !SerializeDelegates.ContainsKey(type);
        
        [ValueSerializer.ExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtendType(Type type)
        {
            var underlyingValueSerializerType = ValueSerializerRegistry.GetValueSerializerForRunType(type.GetGenericArguments()[0]);
            
            SerializeDelegates.Add(
                type, 
                NullableReducer.UnwindSerializeDelegate(ValueSerializerRegistry.CreateSerializeDelegate(underlyingValueSerializerType, type), 
                                                        type)
            );
            
            DeserializeDelegates.Add(
                type, 
                NullableReducer.UnwindDeserializeDelegate(ValueSerializerRegistry.CreateDeserializeDelegate(underlyingValueSerializerType, type), 
                                                          type)
            );
            
            GetSizeFromValueDelegates.Add(
                type, 
                NullableReducer.UnwindGetSizeFromValueDelegate(ValueSerializerRegistry.CreateGetSizeFromValueDelegate(underlyingValueSerializerType, type), 
                                                               type)
            );
            
            GetSizeFromBufferDelegates.Add(type, ValueSerializerRegistry.CreateGetSizeFromBufferDelegate(underlyingValueSerializerType, type));
        }

        /// <summary>
        /// Writes given nullable value to given buffer beginning at given offset if it has value.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T? value, byte[] buffer, int offset) where T : struct
        {
            if (!value.HasValue)
                return;
            
            ((SerializeDelegate<T?>)SerializeDelegates[typeof(T?)])(value, buffer, offset);
        }
        
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as nullable value and returns that value to the caller. This function assumes
        /// there is an actual value in the buffer at given offset.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T? Deserialize<T>(byte[] buffer, int offset) where T : struct
        {
            return ((DeserializeDelegate<T?>)DeserializeDelegates[typeof(T?)])(buffer, offset);
        }

        /// <summary>
        /// Returns size of nullable value from buffer, size will vary. This function assumes there is an actual value in the buffer at given offset.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer<T>(byte[] buffer, int offset) where T : struct
        {
            return ((GetSizeFromBufferDelegate)GetSizeFromBufferDelegates[typeof(T?)])(buffer, offset);
        }
        
        /// <summary>
        /// Returns size of nullable value if it has value, size will vary. Will returns zero if the nullable has no value. 
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T? value) where T : struct
        {
            return (ushort)(!value.HasValue ? 0 : ((GetSizeFromValueDelegate<T?>)GetSizeFromValueDelegates[typeof(T?)])(value));
        }
    }
}