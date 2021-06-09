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
}