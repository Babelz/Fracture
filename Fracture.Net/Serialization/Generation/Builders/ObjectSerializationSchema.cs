using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace Fracture.Net.Serialization.Generation.Builders
{
    public static class ObjectSerializationSchema
    {
        public static void DefineNullable(Type type)
        {
            if (!NullableSerializer.IsNewNullableType(type))
                return;
            
            NullableSerializer.RegisterNullableTypeSerializer(type);
        }
        
        public static void DefineArray(Type type)
        {
            if (NullableSerializer.SupportsType(type.GetElementType()) && NullableSerializer.IsNewNullableType(type.GetElementType()))
                NullableSerializer.RegisterNullableTypeSerializer(type.GetElementType());

            if (!ArraySerializer.IsNewArrayType(type.GetElementType()))
                return;
            
            ArraySerializer.RegisterArrayTypeSerializer(type.GetElementType());
        }
        
        public static void DefineDictionary(Type type)
        {
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (NullableSerializer.SupportsType(genericArgument) && NullableSerializer.IsNewNullableType(genericArgument))
                    NullableSerializer.RegisterNullableTypeSerializer(genericArgument);
                
                if (KeyValuePairSerializer.IsNewSerializationType(genericArgument))
                    KeyValuePairSerializer.RegisterNewSerializationType(genericArgument);
            }
        }
        
        public static void DefineStruct(ObjectSerializationMapper mapper)
        {
            var mapping            = mapper.Map();
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            var serializationOps   = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var deserializationValueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, deserializationOps);
            var serializationValueRanges   = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, serializationOps);

            StructSerializer.RegisterStructTypeSerializer(
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
            
            foreach (var type in mapping.Values.Select(v => v.Type))
            {
                if (NullableSerializer.SupportsType(type))
                    DefineNullable(type);
                else if (ArraySerializer.SupportsType(type))
                    DefineArray(type);
                else if (DictionarySerializer.SupportsType(type))
                    DefineDictionary(type);
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        public sealed class LoadAttribute : Attribute
        {
            public LoadAttribute()
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class SchemaAttribute : Attribute
        {
            #region Properties
            public string Name
            {
                get;
            }
            #endregion

            public SchemaAttribute(string name)
                => Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
        }
    }
}