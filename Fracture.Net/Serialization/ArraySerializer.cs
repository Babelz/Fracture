using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Generic value serializer that provides serialization for arrays of supported types.
    ///
    /// Arrays are structured as follows in the streams:
    ///     [content-length 2-bytes]                                
    ///     [collection-length 2-bytes]
    ///     [sparse-collection-flag 1-bytes]
    ///         if sparse collection:
    ///         [null-mask-length 2-bytes]
    ///         [null-mask, null-mask-length count of bytes]
    ///     [collection-length of elements, each can vary in size]
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
            var elementType = ValueSerializer.GetUnderlyingSerializationType(typeof(T));

            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            // Leave 2-bytes from the beginning of the object for content length.
            var contentLengthOffset = offset;
            offset += Protocol.ContentLength.Size;
            
            // Write collection length header.
            Protocol.CollectionLength.Write(checked((ushort)values.Length), buffer, offset);
            offset += Protocol.CollectionLength.Size;

            // Check for possible null values in the collection and serialize null mask if needed.
            var isSparseCollection = values.Any(v => v == null);

            // Write sparse collection flag.
            Protocol.TypeData.Write((byte)(isSparseCollection ? 1 : 0), buffer, offset);
            offset += Protocol.TypeData.Size;
            
            var nullMaskOffset = isSparseCollection ? offset + Protocol.NullMaskLength.Size : 0;
            var nullMask       = isSparseCollection ? new BitField(checked((ushort)(values.Length))) : default;

            // Leave space for the null mask.
            if (isSparseCollection)
                offset += BitFieldSerializer.GetSizeFromValue(nullMask);
            
            var serializeDelegate        = (SerializeDelegate<T>)SerializeDelegates[elementType];
            var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[elementType];
            var contentLength            = Protocol.TypeData.Size + Protocol.ContentLength.Size + Protocol.CollectionLength.Size + 
                                           (isSparseCollection ? Protocol.NullMaskLength.Size + BitField.LengthFromBits(values.Length) : 0);
            
            for (var i = 0; i < values.Length; i++)
            {
                if (isSparseCollection && values[i] == null)
                    nullMask.SetBit(i, true);
                else
                    serializeDelegate(values[i], buffer, offset);
                
                var valueSize = getSizeFromValueDelegate(values[i]);
                
                offset        += valueSize;
                contentLength += valueSize;
            }
            
            // Write content length at it's offset and add any null related content variables to length.
            Protocol.ContentLength.Write(checked((ushort)contentLength), buffer, contentLengthOffset);
            
            // Write null mask and it's length at it's offset.
            if (isSparseCollection)
                BitFieldSerializer.Serialize(nullMask, buffer, nullMaskOffset);
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as array
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T[] Deserialize<T>(byte[] buffer, int offset)
        {
            var elementType = ValueSerializer.GetUnderlyingSerializationType(typeof(T));
            
            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            // Seek to collection length.
            offset += Protocol.ContentLength.Size;
            
            // Create collection and seek to null mask.
            var values = new T[Protocol.CollectionLength.Read(buffer, offset)];
            offset += Protocol.CollectionLength.Size;
            
            // Determine if the collection is sparse and act accordingly.
            var isSparseCollection = Protocol.TypeData.Read(buffer, offset) == 1;
            var nullMask           = isSparseCollection ? BitFieldSerializer.Deserialize(buffer, offset + Protocol.TypeData.Size) : default;
            
            // If null mask is present, seek to actual data part of the collection.
            offset += isSparseCollection ? BitFieldSerializer.GetSizeFromBuffer(buffer, offset + Protocol.TypeData.Size) : 0;
            
            var deserializeDelegate       = (DeserializeDelegate<T>)DeserializeDelegates[elementType];
            var getSizeFromBufferDelegate = (GetSizeFromBufferDelegate)GetSizeFromBufferDelegates[elementType];

            for (var i = 0; i < values.Length; i++)
            {
                if (isSparseCollection && nullMask.GetBit(i)) 
                    continue;
                
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
            => Protocol.ContentLength.Read(buffer, offset);
                
        /// <summary>
        /// Returns size of array value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T[] values)
        {
            var elementType = ValueSerializer.GetUnderlyingSerializationType(typeof(T));
            
            if (IsNewArrayType(elementType))
                RegisterArrayTypeSerializer(elementType);
            
            var isSparseCollection       = values.Any(v => v == null);
            var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[elementType];
            var size                     = Protocol.TypeData.Size + Protocol.ContentLength.Size + Protocol.CollectionLength.Size + 
                                           (isSparseCollection ? Protocol.NullMaskLength.Size + BitField.LengthFromBits(values.Length) : 0);
            
            for (var i = 0; i < values.Length; i++)
                size += getSizeFromValueDelegate(values[i]);
            
            return checked((ushort)size);
        }
    }
    
    [GenericValueSerializer]
    public static class ListSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(List<>);
        
        /// <summary>
        /// Writes given list to given buffer beginning at given offset. This function allocates new array because it uses the array serializer and will
        /// include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(List<T> value, byte[] buffer, int offset) 
            => ArraySerializer.Serialize(value.ToArray(), buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as list
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static List<T> Deserialize<T>(byte[] buffer, int offset)
            => new List<T>(ArraySerializer.Deserialize<T>(buffer, offset));

        /// <summary>
        /// Returns size of list, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => ArraySerializer.GetSizeFromBuffer(buffer, offset);

        /// <summary>
        /// Returns size of list value, size will vary. This function allocates new array because it uses the array serializer and will include some
        /// overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(List<T> value)
            => ArraySerializer.GetSizeFromValue(value.ToArray());
    }
    
    [GenericValueSerializer]
    public static class KeyValuePairSerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, Delegate> SerializeDelegates        = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> DeserializeDelegates      = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromValueDelegates = new Dictionary<Type, Delegate>();
        #endregion
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNewType(Type type)
            => !SerializeDelegates.ContainsKey(type);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterTypeSerializer(Type serializationType)
        {
            SerializeDelegates.Add(serializationType, 
                                   ValueSerializerRegistry.CreateSerializeDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
            
            DeserializeDelegates.Add(serializationType, 
                                     ValueSerializerRegistry.CreateDeserializeDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
          
            GetSizeFromValueDelegates.Add(serializationType, 
                                          ValueSerializerRegistry.CreateGetSizeFromValueDelegate(ValueSerializerRegistry.GetValueSerializerForRunType(serializationType))
            );
        }
        
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(KeyValuePair<,>);
        
        /// <summary>
        /// Writes given key value pair value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<TKey, TValue>(KeyValuePair<TKey, TValue> value, byte[] buffer, int offset)
        {
            var keyType = ValueSerializer.GetUnderlyingSerializationType(value.Key.GetType());
            
            if (IsNewType(keyType))
                RegisterTypeSerializer(keyType);

            var valueType = ValueSerializer.GetUnderlyingSerializationType(value.Value.GetType());
            
            if (IsNewType(valueType))
                RegisterTypeSerializer(valueType);
            
            // Leave 2-bytes from the beginning of the object for content length.
            var contentLengthOffset = offset;
            offset += Protocol.ContentLength.Size;
            
            var keySerializeDelegate   = (SerializeDelegate<TKey>)SerializeDelegates[keyType];
            var valueSerializeDelegate = (SerializeDelegate<TValue>)SerializeDelegates[valueType];
            
            keySerializeDelegate(value.Key, buffer, offset);
            valueSerializeDelegate(value.Value, buffer, offset + ((GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[valueType])(value.Key));
            
            // Write content length.
            Protocol.ContentLength.Write(GetSizeFromValue(value), buffer, contentLengthOffset);
        }

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as key value pair value
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static KeyValuePair<TKey, TValue> Deserialize<TKey, TValue>(byte[] buffer, int offset)
        {
            var keyType = ValueSerializer.GetUnderlyingSerializationType(typeof(TKey));
            
            if (IsNewType(keyType))
                RegisterTypeSerializer(keyType);

            var valueType = ValueSerializer.GetUnderlyingSerializationType(typeof(TValue));
            
            if (IsNewType(valueType))
                RegisterTypeSerializer(valueType);
            
            // Skip to serialization values.
            offset += Protocol.ContentLength.Size;
            
            var keyDeserializeDelegate   = (DeserializeDelegate<TKey>)DeserializeDelegates[keyType];
            var valueDeserializeDelegate = (DeserializeDelegate<TValue>)DeserializeDelegates[valueType];

            var key   = keyDeserializeDelegate(buffer, offset);
            var value = valueDeserializeDelegate(buffer, offset + ((GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[valueType])(key));
            
            return new KeyValuePair<TKey, TValue>(key, value);
        }
        
        /// <summary>
        /// Returns size of key value pair value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => (ushort)(Protocol.ContentLength.Read(buffer, offset) + Protocol.ContentLength.Size);

        /// <summary>
        /// Returns size of key value pair value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<TKey, TValue>(KeyValuePair<TKey, TValue> value)
            => (ushort)((((GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[typeof(TKey)])(value.Key) + 
                         ((GetSizeFromValueDelegate<TValue>)GetSizeFromValueDelegates[typeof(TValue)])(value.Value)));
    }
    
    [GenericValueSerializer]
    public static class DictionarySerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(Dictionary<,>);
        
        /// <summary>
        /// Writes given dictionary to given buffer beginning at given offset. This function allocates new array because it uses the array serializer
        /// and will include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<TKey, TValue>(Dictionary<TKey, TValue> value, byte[] buffer, int offset) 
            => ArraySerializer.Serialize(value.ToArray(), buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as dictionary and returns that value to the caller. This function allocates new
        /// array because it uses the array serializer and will include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static Dictionary<TKey, TValue> Deserialize<TKey, TValue>(byte[] buffer, int offset)
            => ArraySerializer.Deserialize<KeyValuePair<TKey, TValue>>(buffer, offset).ToDictionary(k => k.Key, v => v.Value);
        
        /// <summary>
        /// Returns size of dictionary, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => ArraySerializer.GetSizeFromBuffer(buffer, offset);

        /// <summary>
        /// Returns size of dictionary value, size will vary. This function allocates new array because it uses the array serializer and will include some
        /// overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<TValue, TKey>(Dictionary<TValue, TKey> value)
            => ArraySerializer.GetSizeFromValue(value.ToArray());
    }
}