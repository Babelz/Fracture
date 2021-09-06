using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Generic value serializer that provides serialization for arrays of supported types.
    /// </summary>
    [GenericValueSerializer]
    public static class ArraySerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, Delegate> SerializeDelegates         = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> DeserializeDelegates       = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromBufferDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromValueDelegates  = new Dictionary<Type, Delegate>();
        #endregion
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNewArrayType(Type type)
            => !SerializeDelegates.ContainsKey(type);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterArrayTypeSerializer(Type serializationType)
        {
            SerializeDelegates.Add(serializationType, 
                                   ValueSerializerRegistry.CreateSerializeDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
            
            DeserializeDelegates.Add(serializationType, 
                                     ValueSerializerRegistry.CreateDeserializeDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
            
            GetSizeFromBufferDelegates.Add(serializationType, 
                                           ValueSerializerRegistry.CreateGetSizeFromBufferDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
            
            GetSizeFromValueDelegates.Add(serializationType, 
                                          ValueSerializerRegistry.CreateGetSizeFromValueDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
        }

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsArray;
        
        /// <summary>
        /// Writes given array to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T[] values, byte[] buffer, int offset) 
        {
            var elementType = typeof(T);
            
            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            var serializeDelegate = (SerializeDelegate<T>)SerializeDelegates[elementType];
            
            // Leave 2-bytes from the beginning of the object for content length.
            var contentLengthOffset = offset;
            offset += Protocol.ContentLength.Size;

            // Write collection length header.
            Protocol.CollectionLength.Write(checked((ushort)values.Length), buffer, offset);
            offset += Protocol.CollectionLength.Size;
            
            var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[elementType];
            var contentLength            = 0u;
            
            for (var i = 0; i < values.Length; i++)
            {
                serializeDelegate(values[i], buffer, offset);
                
                var valueSize = getSizeFromValueDelegate(values[i]);
                
                offset        += valueSize;
                contentLength += valueSize;
            }
            
            // Write content length at it's offset.
            Protocol.ContentLength.Write(checked((ushort)contentLength), buffer, contentLengthOffset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as array
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T[] Deserialize<T>(byte[] buffer, int offset)
        {
            var elementType = typeof(T);
            
            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            // Seek to collection length.
            offset += Protocol.ContentLength.Size;
            
            var deserializeDelegate       = (DeserializeDelegate<T>)DeserializeDelegates[elementType];
            var getSizeFromBufferDelegate = (GetSizeFromBufferDelegate)GetSizeFromBufferDelegates[elementType];
            var values                    = new T[Protocol.CollectionLength.Read(buffer, offset)];
            
            // Seek to actual data.
            offset += Protocol.CollectionLength.Size;
            
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = deserializeDelegate(buffer, offset);
                
                offset += getSizeFromBufferDelegate(buffer, offset);
            }
            
            return values;
        }

        /// <summary>
        /// Returns size of array, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(Protocol.ContentLength.Read(buffer, offset) + Protocol.ContentLength.Size + Protocol.CollectionLength.Size);
                
        /// <summary>
        /// Returns size of array value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T[] values)
        {
            var elementType = typeof(T);
            
            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[elementType];
            var size                     = (ushort)(Protocol.CollectionLength.Size + Protocol.ContentLength.Size);
            
            for (var i = 0; i < values.Length; i++)
                size += getSizeFromValueDelegate(values[i]);
            
            return size;
        }
    }
    
    [GenericValueSerializer]
    public static class ListSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(List<>);
        
        /// <summary>
        /// Writes given list to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(List<T> value, byte[] buffer, int offset) 
        {
            throw new NotImplementedException();
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as list
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static List<T> Deserialize<T>(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of list, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of list value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(List<T> value)
        {
            throw new NotImplementedException();
        }
    }
}