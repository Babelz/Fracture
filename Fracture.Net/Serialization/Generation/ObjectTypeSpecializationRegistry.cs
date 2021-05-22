namespace Fracture.Net.Serialization.Generation
{
    using System;
    using System.Collections.Generic;

    namespace Fracture.Net.Serialization.Generation
    {
        /// <summary>
        /// Class that handles specialized type mappings from run time types to serialization specialization types.
        /// </summary>
        public sealed class ObjectTypeSpecializationRegistry
        {
            #region Fields
            private readonly Dictionary<ushort, Type> serializationTypeMapping;
            private readonly Dictionary<Type, ushort> runTypeMapping; 
        
            // Specialization type id counter used for generating new specialization ids.
            private ushort nextSpecializationTypeId;
            #endregion

            public ObjectTypeSpecializationRegistry()
            {
                serializationTypeMapping = new Dictionary<ushort, Type>();
                runTypeMapping           = new Dictionary<Type, ushort>();
            }
        
            public void SpecializeRunType(Type type)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));
            
                serializationTypeMapping.Add(nextSpecializationTypeId, type);
                runTypeMapping.Add(type, nextSpecializationTypeId);
            
                nextSpecializationTypeId++;
            }
        
            public bool IsSpecializedRunType(Type type)
                => runTypeMapping.ContainsKey(type);
        
            public bool IsSpecializedSerializationType(ushort specializationTypeId)
                => serializationTypeMapping.ContainsKey(specializationTypeId);
        
            public ushort GetSpecializedRunTypeSerializationTypeId(Type type)
                => runTypeMapping[type];
        
            public Type GetSpecializedSerializationRunType(ushort specializationTypeId)
                => serializationTypeMapping[specializationTypeId];
        }
    }
}