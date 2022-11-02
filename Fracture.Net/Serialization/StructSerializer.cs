using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Net.Serialization.Generation;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Exception that occurs when no run type is mapped for serialization type. 
    /// </summary>
    public sealed class RunTypeNotMappedException : Exception
    {
        public RunTypeNotMappedException(ushort serializationTypeId)
            : base($"no run type is mapped for serialization type {serializationTypeId}")
            => Data["SerializationTypeId"] = serializationTypeId;
    }

    /// <summary>
    /// Exception that occurs when no serialization type is mapped for run type.
    /// </summary>
    public sealed class SerializationTypeNotMappedException : Exception
    {
        public SerializationTypeNotMappedException(Type serializationType)
            : base($"no serialization type is mapped for run type {serializationType?.Name ?? "null"}")
            => Data["SerializationType"] = serializationType;
    }

    /// <summary>
    /// Generic value serializer that provides serialization for structure types. Structure types are any user defined value types or structures that can be
    /// mapped for serialization. The serializer can be used for serialization and deserialization in two ways:
    ///     - Type information is passed via generic interface when the type is known at runtime
    ///     - Type information is not passed and object value will be used instead
    ///
    /// The serializer stores runtime type information about the serialized types to binary format for keeping the type information between serialization calls.
    /// Because the actual serialization delegates are generated dynamically at runtime, values are always boxed when they are serialized or deserialized.
    /// </summary>
    [GenericValueSerializer]
    public static class StructSerializer
    {
        #region Static fields
        private static readonly Dictionary<Type, ushort> SerializationTypeIdMappings = new Dictionary<Type, ushort>();
        private static readonly Dictionary<ushort, Type> RunTypeMappings             = new Dictionary<ushort, Type>();

        private static readonly Dictionary<Type, ObjectSerializer> Serializers = new Dictionary<Type, ObjectSerializer>();

        private static ushort NextSerializationTypeId;
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort InternalGetSizeFromValue(Type serializationType, object value)
            => checked((ushort)(Serializers[serializationType].GetSizeFromValue(value) + Protocol.ContentLength.Size + Protocol.SerializationTypeId.Size));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object InternalDeserialize(byte[] buffer, int offset)
        {
            offset += Protocol.ContentLength.Size;

            var serializationTypeId = Protocol.SerializationTypeId.Read(buffer, offset);
            offset += Protocol.SerializationTypeId.Size;

            if (!RunTypeMappings.ContainsKey(serializationTypeId))
                throw new RunTypeNotMappedException(serializationTypeId);

            var runType = RunTypeMappings[serializationTypeId];

            return Serializers[runType].Deserialize(buffer, offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalSerialize(Type serializationType, object value, byte[] buffer, int offset)
        {
            if (!SerializationTypeIdMappings.ContainsKey(serializationType))
                throw new SerializationTypeNotMappedException(serializationType);

            var serializationTypeId = SerializationTypeIdMappings[serializationType];

            var contentLength = checked((ushort)(Serializers[serializationType].GetSizeFromValue(value) +
                                                 Protocol.ContentLength.Size +
                                                 Protocol.SerializationTypeId.Size));

            Protocol.ContentLength.Write(contentLength, buffer, offset);
            offset += Protocol.ContentLength.Size;

            Protocol.SerializationTypeId.Write(serializationTypeId, buffer, offset);
            offset += Protocol.SerializationTypeId.Size;

            Serializers[serializationType].Serialize(value, buffer, offset);
        }

        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => Serializers.ContainsKey(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Map(ObjectSerializationMapping mapping)
        {
            if (SupportsType(mapping.Type))
            {
                throw new InvalidOperationException("serialization type already registered")
                {
                    Data = { { nameof(mapping.Type), mapping.Type } },
                };
            }

            var program    = ObjectSerializerCompiler.CompileSerializerProgram(mapping);
            var serializer = ObjectSerializerInterpreter.InterpretSerializer(program);

            Serializers.Add(mapping.Type, serializer);

            SerializationTypeIdMappings.Add(mapping.Type, NextSerializationTypeId);
            RunTypeMappings.Add(NextSerializationTypeId, mapping.Type);

            NextSerializationTypeId++;
        }

        /// <summary>
        /// Writes given structure to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T value, byte[] buffer, int offset)
            => InternalSerialize(value.GetType(), value, buffer, offset);

        /// <summary>
        /// Writes given structure to given buffer beginning at given offset.
        /// </summary>
        public static void Serialize(object value, byte[] buffer, int offset)
            => InternalSerialize(value.GetType(), value, buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as structure and returns that value to the caller. Type information is retrieved the
        /// buffer for the object.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T Deserialize<T>(byte[] buffer, int offset)
            => (T)InternalDeserialize(buffer, offset);

        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as structure and returns that value to the caller. Type information is retrieved the
        /// buffer for the object.
        /// </summary>
        public static object Deserialize(byte[] buffer, int offset)
            => InternalDeserialize(buffer, offset);

        /// <summary>
        /// Returns size of structure, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
            => Protocol.ContentLength.Read(buffer, offset);

        /// <summary>
        /// Returns size of structure value, size will vary. Type information is retrieved from the proved generic type argument.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T value)
            => InternalGetSizeFromValue(value.GetType(), value);

        /// <summary>
        /// Returns size of structure value, size will vary. Type information is retrieved from the provided object.
        /// </summary>
        public static ushort GetSizeFromValue(object value)
            => InternalGetSizeFromValue(value.GetType(), value);
    }
}