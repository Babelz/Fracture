using System;
using System.ComponentModel;
using System.Linq;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;
using Fracture.Net.Tests.Serialization.Generation;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Category("Serialization")]
    public class StructSerializerTests
    {
        #region Test types
        // TODO: make this a struct and see everything burn - investigate, test and fix.
        public class Vec2
        {
            #region Fields
            public readonly float X;
            public readonly float Y;
            #endregion

            public Vec2(float x, float y)
            {
                X = x;
                Y = y;
            }
        }

        public sealed class ClassComposedOfStructures
        {
            #region Properties
            public Vec2 X;
            public Vec2? Y;
            public Vec2? Z;
            #endregion
        }
        #endregion

        private static void CompileSerializer(ObjectSerializationMapping mapping)
        {
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            var serializationOps   = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var deserializationValueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, deserializationOps);
            var serializationValueRanges   = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(mapping.Type, serializationOps);

            StructSerializer.RegisterStructureTypeSerializer(
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

        static StructSerializerTests()
        {
            CompileSerializer(ObjectSerializationMapper.Create()
                                                       .FromType<Vec2>()
                                                       .PublicFields()
                                                       .ParametrizedActivation(ObjectActivationHint.Field("x", "X"), ObjectActivationHint.Field("y", "Y"))
                                                       .Map());
            
            CompileSerializer(ObjectSerializationMapper.Create()
                                                       .FromType<ClassComposedOfStructures>()
                                                       .PublicFields()
                                                       .Map());
        }
        
        [Fact]
        public void Register_Throws_If_Type_Is_Already_Registered()
            => Assert.IsType<InvalidOperationException>(Record.Exception(() => StructSerializer.RegisterStructureTypeSerializer(typeof(Vec2), null, null, null)));
        
        [Fact]
        public void Serializes_Structures_Composed_Of_Structures_Back_And_Forth()
        {
            var testValueIn = new ClassComposedOfStructures()
            {
                X = new Vec2(1.0f, 2.0f),
                Z = new Vec2(3.0f, 4.0f)
            };
            
            var buffer = new byte[128];
            
            StructSerializer.Serialize(testValueIn, buffer, 0);
            
            var testValueOut = (ClassComposedOfStructures)StructSerializer.Deserialize(buffer, 0);
            
            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }
    }
}