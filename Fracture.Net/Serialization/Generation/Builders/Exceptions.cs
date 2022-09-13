using System;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Exception thrown when dynamic object serialization throws an exception.
    /// </summary>
    [Serializable]
    public sealed class DynamicSerializeException : Exception
    {
        public DynamicSerializeException(Type serializationType, Exception innerException, object value)
            : base($"exception occurred while serializing object {serializationType.FullName}", innerException)
        {
            Data["SerializationType"] = serializationType;
            Data["Value"]             = value;
        }
    }

    /// <summary>
    /// Exception thrown when dynamic object deserialization throws an exception.
    /// </summary>
    [Serializable]
    public sealed class DynamicDeserializeException : Exception
    {
        public DynamicDeserializeException(Type serializationType, Exception innerException, byte [] buffer, int offset)
            : base($"exception occurred while deserializing object {serializationType.FullName}", innerException)
        {
            Data["SerializationType"] = serializationType;
            Data["Buffer"]            = buffer;
            Data["Offset"]            = offset;
        }
    }

    /// <summary>
    /// Exception thrown when dynamic get size from value throws an exception.
    /// </summary>
    [Serializable]
    public sealed class DynamicGetSizeFromValueException : Exception
    {
        public DynamicGetSizeFromValueException(Type serializationType, Exception innerException, object value)
            : base($"exception occurred while computing object size {serializationType.FullName}", innerException)
        {
            Data["SerializationType"] = serializationType;
            Data["Value"]             = value;
        }
    }
}