using System;

namespace Fracture.Net.Serialization
{
    public sealed class SerializationTypeException : Exception
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }
        #endregion

        public SerializationTypeException(string message, Type serializationType)
            : base($"{serializationType.Name} caused an exception: {message}")
        {
            SerializationType = serializationType;
        }
    }
    
    public sealed class ValueSerializerSchemaException : Exception
    {
        #region Properties
        public Type ValueSerializerType
        {
            get;
        }
        #endregion

        public ValueSerializerSchemaException(string message, Type valueSerializerType)
            : base($"{valueSerializerType.Name} caused an exception: {message}")
        {
            ValueSerializerType = valueSerializerType;
        }
    }
}