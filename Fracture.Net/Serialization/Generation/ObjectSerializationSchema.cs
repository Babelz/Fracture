using System;
using System.Linq;
using System.Reflection;
using NLog;
using NLog.Fluent;

namespace Fracture.Net.Serialization.Generation
{
    public static class ObjectSerializationSchema
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

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
        
        public static void Load(string name) 
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                try
                {
                    var schemaType = assembly.GetTypes().FirstOrDefault(t => (t.GetCustomAttribute<SchemaAttribute>()?.Name == name));

                    if (schemaType == null) continue;
                    
                    Log.Info($"loading serialization schema {name}");
                    
                    var loadMethod = schemaType.GetMethods(BindingFlags.Instance |
                                                           BindingFlags.Public | 
                                                           BindingFlags.Static |
                                                           BindingFlags.NonPublic).FirstOrDefault(m => m.GetCustomAttribute<LoadAttribute>() != null);
                    
                    if (loadMethod == null)
                        throw new InvalidOperationException($"serialization schema with name \"{name}\" has no method annotated with {nameof(LoadAttribute)}"); 
                    
                    loadMethod.Invoke(null, null);
                    
                    return;
                }   
                catch (ReflectionTypeLoadException e)
                {
                    Log.Warn(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            }
            
            throw new InvalidOperationException($"no serialization schema with name \"{name}\" was found");
        }
    }
}