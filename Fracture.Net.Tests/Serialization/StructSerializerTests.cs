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
        private struct Vec2
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

        private sealed class ClassComposedOfStructs
        {
            #region Properties
            public Vec2 X;
            public Vec2? Y;
            public Vec2? Z;
            #endregion
        }
        
        private struct Inner
        {
            #region Fields
            public Inner1 Value;
            #endregion
        }
        
        private struct Inner1
        {
            #region Fields
            public Inner2 Inner;
            #endregion
        }
        
        private struct Inner2
        {
            #region Fields
            public Inner3 Inner;
            #endregion
        }
        
        private struct Inner3
        {
            #region Fields
            public Inner4 Inner;
            #endregion
        }
        
        private struct Inner4
        {
            #region Fields
            public Vec2 Value;
            #endregion
        }
        
        private struct DeepInnerTestStruct
        {
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
                                                       .FromType<ClassComposedOfStructs>()
                                                       .PublicFields()
                                                       .Map());
            
            CompileSerializer(ObjectSerializationMapper.Create().FromType<Inner4>().PublicFields().Map());
            CompileSerializer(ObjectSerializationMapper.Create().FromType<Inner3>().PublicFields().Map());
            CompileSerializer(ObjectSerializationMapper.Create().FromType<Inner2>().PublicFields().Map());
            CompileSerializer(ObjectSerializationMapper.Create().FromType<Inner1>().PublicFields().Map());
            CompileSerializer(ObjectSerializationMapper.Create().FromType<Inner>().PublicFields().Map());
        }
        
        [Fact]
        public void Register_Throws_If_Type_Is_Already_Registered()
            => Assert.IsType<InvalidOperationException>(Record.Exception(() => StructSerializer.RegisterStructureTypeSerializer(typeof(Vec2), null, null, null)));
        
        [Fact]
        public void Serializes_Structures_Composed_Of_Structures_Back_And_Forth()
        {
            var testValueIn = new ClassComposedOfStructs()
            {
                X = new Vec2(1.0f, 2.0f),
                Z = new Vec2(3.0f, 4.0f)
            };
            
            var buffer = new byte[128];
            
            StructSerializer.Serialize(testValueIn, buffer, 0);
            
            var testValueOut = StructSerializer.Deserialize<ClassComposedOfStructs>(buffer, 0);
            
            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }
        
        [Fact]
        public void Serializes_Complex_Nested_Structures_Composed_Of_Structures_Back_And_Forth()
        {
            var testValueIn = new Inner()
            {
                Value = new Inner1()
                {
                    Inner = new Inner2()
                    {
                        Inner = new Inner3()
                        {
                            Inner = new Inner4()
                            {
                                Value = new Vec2(float.MinValue, float.MaxValue)
                            }
                        }
                    }
                }
            };
            
            var buffer = new byte[128];
            
            StructSerializer.Serialize(testValueIn, buffer, 0);
            
            var testValueOut = StructSerializer.Deserialize<Inner>(buffer, 0);
            
            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }
    }
}