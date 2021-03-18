using System;
using System.Runtime.Serialization;

namespace Fracture.Common.Di
{
    [Serializable]
    public sealed class DependencyNotFoundException : Exception
    {
        public DependencyNotFoundException()
        {
        }

        public DependencyNotFoundException(Type type)
            : base($"dependency of type {type.Name} not found")
        {
        }

        private DependencyNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }

    [Serializable]
    public sealed class DependencyBinderException : Exception
    {
        #region Properties
        public Type Type
        {
            get;
        }
        #endregion
        
        public DependencyBinderException(Type type, string message)
            : base(message)
        {
            Type = type;
        }

        public DependencyBinderException(Type type, string message, Exception innerException)
            : base(message, innerException)
        {
            Type = type;
        }

        private DependencyBinderException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
