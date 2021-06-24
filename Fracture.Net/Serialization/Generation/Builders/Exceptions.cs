using System;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Exception thrown when dynamic object serialization throws an exception.
    /// </summary>
    public sealed class DynamicSerializeException : Exception
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }

        public object Value
        {
            get;
        }
        #endregion

        public DynamicSerializeException(Type serializationType, Exception innerException, object value)
            : base($"exception occurred while serializing object {serializationType.Namespace}", innerException)
        {
            SerializationType = serializationType;
            Value             = value;
        }
    }
    
    /// <summary>
    /// Exception thrown when dynamic object deserialization throws an exception.
    /// </summary>
    public sealed class DynamicDeserializeException : Exception
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }

        public byte[] Buffer
        {
            get;
        }

        public int Offset
        {
            get;
        }
        #endregion

        public DynamicDeserializeException(Type serializationType, Exception innerException, byte[] buffer, int offset)
            : base($"exception occurred while deserializing object {serializationType.Namespace}", innerException)
        {
            SerializationType = serializationType;
            Buffer            = buffer;
            Offset            = offset;
        }
    }
    
    /// <summary>
    /// Exception thrown when dynamic get size from value throws an exception.
    /// </summary>
    public sealed class DynamicGetSizeFromValueException : Exception
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }

        public object Value
        {
            get;
        }
        #endregion

        public DynamicGetSizeFromValueException(Type serializationType, Exception innerException, object value)
            : base($"exception occurred while computing object size {serializationType.Namespace}", innerException)
        {
            SerializationType = serializationType;
            Value             = value;
        }
    }
}