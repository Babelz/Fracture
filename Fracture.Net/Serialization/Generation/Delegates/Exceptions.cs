using System;

namespace Fracture.Net.Serialization.Generation.Delegates
{
    public sealed class SerializationException : Exception
    {
        #region Properties
        public Type SerializationType
        {
            get;
        }
        #endregion

        public SerializationException(Type serializationType, Exception innerException, string message)
            : base($"serialization exception occurred for type {serializationType.Namespace}: ${message}", innerException)
        {
            SerializationType = serializationType;
        }
    }
}