using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Serializer that provides generic structure and class serialization. Uses internal caching and code generation
    /// for speeding up serialization. This class can serve as top level serializer. Operations of this serializer are
    /// thread safe.
    /// </summary>
    public sealed class StructureSerializer : ValueSerializer
    {
        #region Static fields
        private static readonly object Padlock = new object();
        #endregion
        
        private StructureSerializer(Type runtimeType, params string[] properties) 
            : base(SerializationType.Structure)
        {
            // Manual map properties.
            
            // Create codegen backend if it does not exist.
            
            // Get codegen backend.
        }
        
        private StructureSerializer(Type runtimeType)
            : this(runtimeType, null)
        {
        }

        public override bool SupportsType(Type type)
        {
            throw new NotImplementedException();
        }

        public override ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        public override ushort GetSizeFromValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}