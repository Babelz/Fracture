using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Net.Serialization.Generation.Builders;
using NLog;
using NLog.Fluent;

namespace Fracture.Net.Serialization.Generation
{
    public static class ObjectSerializationSchema
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DefineStruct(Type type,
                                        DynamicSerializeDelegate serializeDelegate,
                                        DynamicDeserializeDelegate deserializeDelegate,
                                        DynamicGetSizeFromValueDelegate getSizeFromValueDelegate)
        {
            StructSerializer.RegisterStructTypeSerializer(
                type,
                serializeDelegate,
                deserializeDelegate,
                getSizeFromValueDelegate
            );
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DefineStruct(ObjectSerializationMapper mapper)
        {
            var mapping            = mapper.Map();
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            var serializationOps   = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var deserializationValueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, deserializationOps);
            var serializationValueRanges   = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, serializationOps);

            DefineStruct(
                mapping.Type,
                ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                    serializationValueRanges,
                    mapping.Type,
                    serializationOps
                ),
                ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                    deserializationValueRanges,
                    mapping.Type,
                    deserializationOps
                ),
                ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                    serializationValueRanges,
                    mapping.Type,
                    serializationOps
                ));
        }
    }
}