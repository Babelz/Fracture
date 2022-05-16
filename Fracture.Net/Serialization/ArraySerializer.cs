using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

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
    [ExtendableValueSerializer]
    public static class ArraySerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, Delegate> SerializeDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> DeserializeDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromBufferDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromValueDelegates = new Dictionary<Type, Delegate>();
        #endregion

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsArray;

        [ValueSerializer.CanExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanExtendType(Type type)
            => SupportsType(type) && !SerializeDelegates.ContainsKey(type.GetElementType()!);

        [ValueSerializer.ExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtendType(Type type)
        {
            var elementType         = type.GetElementType();
            var valueSerializerType = ValueSerializerRegistry.GetValueSerializerForRunType(type.GetElementType());

            SerializeDelegates.Add(elementType, ValueSerializerRegistry.CreateSerializeDelegate(valueSerializerType, elementType));
            DeserializeDelegates.Add(elementType, ValueSerializerRegistry.CreateDeserializeDelegate(valueSerializerType, elementType));
            GetSizeFromValueDelegates.Add(elementType, ValueSerializerRegistry.CreateGetSizeFromValueDelegate(valueSerializerType, elementType));
            GetSizeFromBufferDelegates.Add(elementType, ValueSerializerRegistry.CreateGetSizeFromBufferDelegate(valueSerializerType, elementType));
        }

        /// <summary>
        /// Writes given array to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T [] values, byte [] buffer, int offset)
        {
            // Leave 2-bytes from the beginning of the object for content length.
            var contentLengthOffset = offset;
            offset += Protocol.ContentLength.Size;

            // Write collection length header.
            Protocol.CollectionLength.Write(checked((ushort)values.Length), buffer, offset);
            offset += Protocol.CollectionLength.Size;

            // Check for possible null values in the collection and serialize null mask if needed.
            var isSparseCollection           = false;
            var isSparseCollectionFlagOffset = offset;

            offset += Protocol.TypeData.Size;

            // Write sparse collection flag.
            var nullMaskOffset = offset;
            var nullMask       = new BitField(checked((ushort)(BitField.LengthFromBits(values.Length))));

            // Leave space for the null mask.
            var collectionDataRegionOffset = offset;
            var serializeDelegate          = (SerializeDelegate<T>)SerializeDelegates[typeof(T)];
            var getSizeFromValueDelegate   = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[typeof(T)];
            var contentLength              = 0;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    isSparseCollection = true;

                    nullMask.SetBit(i, true);

                    continue;
                }

                serializeDelegate(values[i], buffer, offset);

                var valueSize = getSizeFromValueDelegate(values[i]);

                offset        += valueSize;
                contentLength += valueSize;
            }

            // Write content length at its offset and add any null related content variables to length.
            if (isSparseCollection)
            {
                // Move data region to leave space for null mask.
                Array.Copy(buffer,
                           collectionDataRegionOffset,
                           buffer,
                           collectionDataRegionOffset + BitFieldSerializer.GetSizeFromValue(nullMask),
                           contentLength);

                MemoryMapper.Set(buffer, collectionDataRegionOffset, collectionDataRegionOffset + BitFieldSerializer.GetSizeFromValue(nullMask), 0);

                // Write null mask.
                BitFieldSerializer.Serialize(nullMask, buffer, nullMaskOffset);

                contentLength += BitFieldSerializer.GetSizeFromValue(nullMask);
            }

            // Add header sizes to content length.
            contentLength += Protocol.ContentLength.Size + Protocol.CollectionLength.Size + Protocol.TypeData.Size;

            Protocol.ContentLength.Write(checked((ushort)contentLength), buffer, contentLengthOffset);

            // Write null mask and its length at its offset.
            Protocol.TypeData.Write((byte)(isSparseCollection ? 1 : 0), buffer, isSparseCollectionFlagOffset);
        }

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as array
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T [] Deserialize<T>(byte [] buffer, int offset)
        {
            // Seek to collection length.
            offset += Protocol.ContentLength.Size;

            // Create collection and seek to null mask.
            var values = new T[Protocol.CollectionLength.Read(buffer, offset)];
            offset += Protocol.CollectionLength.Size;

            // Determine if the collection is sparse and act accordingly.
            var isSparseCollection = Protocol.TypeData.Read(buffer, offset) == 1;
            offset += Protocol.TypeData.Size;

            // If null mask is present, seek to actual data part of the collection.
            var nullMask = isSparseCollection ? BitFieldSerializer.Deserialize(buffer, offset) : default;
            offset += isSparseCollection ? BitFieldSerializer.GetSizeFromBuffer(buffer, offset) : 0;

            var deserializeDelegate       = (DeserializeDelegate<T>)DeserializeDelegates[typeof(T)];
            var getSizeFromBufferDelegate = (GetSizeFromBufferDelegate)GetSizeFromBufferDelegates[typeof(T)];

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
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);

        /// <summary>
        /// Returns size of array value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T [] values)
        {
            var isSparseCollection       = false;
            var getSizeFromValueDelegate = (GetSizeFromValueDelegate<T>)GetSizeFromValueDelegates[typeof(T)];
            var size                     = Protocol.ContentLength.Size + Protocol.CollectionLength.Size + Protocol.TypeData.Size;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                {
                    isSparseCollection = true;

                    continue;
                }

                size += getSizeFromValueDelegate(values[i]);
            }

            // Add bit field content length + bytes length if sparse collection.
            if (isSparseCollection)
                size += Protocol.ContentLength.Size + BitField.LengthFromBits(values.Length);

            return checked((ushort)size);
        }
    }

    [GenericValueSerializer]
    [ExtendableValueSerializer]
    public static class ListSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        [ValueSerializer.CanExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanExtendType(Type type)
            => ArraySerializer.CanExtendType(type.GetGenericArguments()[0].MakeArrayType());

        [ValueSerializer.ExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtendType(Type type)
            => ArraySerializer.ExtendType(type.GetGenericArguments()[0].MakeArrayType());

        /// <summary>
        /// Writes given list to given buffer beginning at given offset. This function allocates new array because it uses the array serializer and will
        /// include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(List<T> value, byte [] buffer, int offset)
            => ArraySerializer.Serialize(value.ToArray(), buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as list
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static List<T> Deserialize<T>(byte [] buffer, int offset)
            => new List<T>(ArraySerializer.Deserialize<T>(buffer, offset));

        /// <summary>
        /// Returns size of list, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
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
    [ExtendableValueSerializer]
    public static class KeyValuePairSerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, Delegate> SerializeDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> DeserializeDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> GetSizeFromValueDelegates = new Dictionary<Type, Delegate>();
        #endregion

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);

        [ValueSerializer.CanExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanExtendType(Type type)
            => SupportsType(type) && type.GetGenericArguments().Any(t => !SerializeDelegates.ContainsKey(t));

        [ValueSerializer.ExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtendType(Type type)
        {
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (SerializeDelegates.ContainsKey(genericArgument))
                    continue;

                var valueSerializerType = ValueSerializerRegistry.GetValueSerializerForRunType(genericArgument);

                SerializeDelegates.Add(genericArgument, ValueSerializerRegistry.CreateSerializeDelegate(valueSerializerType, genericArgument));
                DeserializeDelegates.Add(genericArgument, ValueSerializerRegistry.CreateDeserializeDelegate(valueSerializerType, genericArgument));
                GetSizeFromValueDelegates.Add(genericArgument, ValueSerializerRegistry.CreateGetSizeFromValueDelegate(valueSerializerType, genericArgument));
            }
        }

        /// <summary>
        /// Writes given key value pair value to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<TKey, TValue>(KeyValuePair<TKey, TValue> value, byte [] buffer, int offset)
        {
            var keyType   = typeof(TKey);
            var valueType = typeof(TValue);

            // Leave 2-bytes from the beginning of the object for content length.
            var contentLengthOffset = offset;
            offset += Protocol.ContentLength.Size;

            var keySerializeDelegate   = (SerializeDelegate<TKey>)SerializeDelegates[keyType];
            var valueSerializeDelegate = (SerializeDelegate<TValue>)SerializeDelegates[valueType];

            keySerializeDelegate(value.Key, buffer, offset);
            offset += ((GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[keyType])(value.Key);

            var valueNullFlag = (byte)(value.Value == null ? 1 : 0);

            Protocol.TypeData.Write(valueNullFlag, buffer, offset);
            offset += Protocol.TypeData.Size;

            if (value.Value != null)
                valueSerializeDelegate(value.Value, buffer, offset);

            // Write content length.
            Protocol.ContentLength.Write(GetSizeFromValue(value), buffer, contentLengthOffset);
        }

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as key value pair value
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static KeyValuePair<TKey, TValue> Deserialize<TKey, TValue>(byte [] buffer, int offset)
        {
            var keyType   = typeof(TKey);
            var valueType = typeof(TValue);

            // Skip to serialization values.
            offset += Protocol.ContentLength.Size;

            var keyDeserializeDelegate      = (DeserializeDelegate<TKey>)DeserializeDelegates[keyType];
            var keyGetSizeFromValueDelegate = (GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[keyType];
            var valueDeserializeDelegate    = (DeserializeDelegate<TValue>)DeserializeDelegates[valueType];

            var key = keyDeserializeDelegate(buffer, offset);
            offset += keyGetSizeFromValueDelegate(key);

            var valueNullFlag = Protocol.TypeData.Read(buffer, offset) == 1;
            offset += Protocol.TypeData.Size;

            var value = valueNullFlag ? default : valueDeserializeDelegate(buffer, offset);

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        /// <summary>
        /// Returns size of key value pair value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);

        /// <summary>
        /// Returns size of key value pair value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<TKey, TValue>(KeyValuePair<TKey, TValue> value)
        {
            var valueSize = value.Value != null ? ((GetSizeFromValueDelegate<TValue>)GetSizeFromValueDelegates[typeof(TValue)])(value.Value) : 0;
            var keySize   = ((GetSizeFromValueDelegate<TKey>)GetSizeFromValueDelegates[typeof(TKey)])(value.Key);

            return checked((ushort)(valueSize + keySize + Protocol.ContentLength.Size + Protocol.TypeData.Size));
        }
    }

    [GenericValueSerializer]
    [ExtendableValueSerializer]
    public static class DictionarySerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        [ValueSerializer.CanExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanExtendType(Type type)
            => ArraySerializer.CanExtendType(typeof(KeyValuePair<,>).MakeGenericType(type.GetGenericArguments()).MakeArrayType());

        [ValueSerializer.ExtendType]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtendType(Type type)
        {
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(type.GetGenericArguments());

            if (KeyValuePairSerializer.CanExtendType(keyValuePairType))
                KeyValuePairSerializer.ExtendType(keyValuePairType);

            ArraySerializer.ExtendType(keyValuePairType.MakeArrayType());
        }

        /// <summary>
        /// Writes given dictionary to given buffer beginning at given offset. This function allocates new array because it uses the array serializer
        /// and will include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<TKey, TValue>(Dictionary<TKey, TValue> value, byte [] buffer, int offset)
            => ArraySerializer.Serialize(value.ToArray(), buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as dictionary and returns that value to the caller. This function allocates new
        /// array because it uses the array serializer and will include some overhead because of that. To avoid this use more optimized types.
        ///
        /// TODO: fix the possible performance issue if needed.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static Dictionary<TKey, TValue> Deserialize<TKey, TValue>(byte [] buffer, int offset)
            => ArraySerializer.Deserialize<KeyValuePair<TKey, TValue>>(buffer, offset).ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        /// Returns size of dictionary, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte [] buffer, int offset)
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