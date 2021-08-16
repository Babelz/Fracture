using System;

namespace Fracture.Net.Serialization
{
    [Serializable]
    public sealed class SerializationTypeException : Exception
    {
        public SerializationTypeException(string message, Type serializationType)
            : base($"{serializationType.Name} caused an exception: {message}")
        {
            Data["SerializationType"] = serializationType;
        }
    }
    
    [Serializable]
    public sealed class ValueSerializerSchemaException : Exception
    {
        public ValueSerializerSchemaException(string message, Type valueSerializerType)
            : base($"{valueSerializerType.Name} caused an exception: {message}")
        {
            Data["ValueSerializerType"] = valueSerializerType;
        }
    }
}