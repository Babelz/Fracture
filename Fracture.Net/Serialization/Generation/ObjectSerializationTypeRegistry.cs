using System;
using System.Collections.Generic;

namespace Fracture.Net.Serialization.Generation
{
    namespace Fracture.Net.Serialization.Generation
    {
        /// <summary>
        /// Class that handles specialized type mappings from run time types to serialization specialization types.
        /// </summary>
        public sealed class ObjectSerializationTypeRegistry
        {
            #region Fields
            private readonly Dictionary<ushort, Type> serializationTypeMapping;
            private readonly Dictionary<Type, ushort> runTypeMapping; 
        
            // Specialization type id counter used for generating new specialization ids.
            private ushort nextSpecializationTypeId;
            #endregion

            public ObjectSerializationTypeRegistry()
            {
                serializationTypeMapping = new Dictionary<ushort, Type>();
                runTypeMapping           = new Dictionary<Type, ushort>();
            }
        
            public void Specialize(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));
            
                if (runTypeMapping.ContainsKey(type)) 
                    throw new SerializationTypeException("type is already specialized", type);
                
                serializationTypeMapping.Add(nextSpecializationTypeId, type);
                runTypeMapping.Add(type, nextSpecializationTypeId);
            
                nextSpecializationTypeId++;
            }
        
            public bool IsSpecializedRunType(Type type)
                => runTypeMapping.ContainsKey(type);
        
            public bool IsSpecializedSerializationType(ushort serializationTypeId)
                => serializationTypeMapping.ContainsKey(serializationTypeId);

            public ushort GetSerializationTypeId(Type type)
                => runTypeMapping[type];
            
            public Type GetRunType(ushort serializationType)
                => serializationTypeMapping[serializationType];
        }
    }
}