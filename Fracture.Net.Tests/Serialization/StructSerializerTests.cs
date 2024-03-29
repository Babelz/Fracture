using System;
using System.ComponentModel;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Newtonsoft.Json;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Category("Serialization")]
    public class StructSerializerTests
    {
        #region Test types
        private readonly struct Vec2
        {
            #region Fields
            // ReSharper disable once MemberCanBePrivate.Local - disabled for testing.
            public readonly float X;

            // ReSharper disable once MemberCanBePrivate.Local - disabled for testing.
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
            public Vec2  X;
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

        private sealed class IndirectlyActivatedTestClass
        {
            #region Fields
            public int X;
            public int Y;
            #endregion

            public IndirectlyActivatedTestClass()
            {
            }
        }
        #endregion

        static StructSerializerTests()
        {
            StructSerializer.Map(ObjectSerializationMapper.ForType<Vec2>()
                                                          .PublicFields()
                                                          .ParametrizedActivation(ObjectActivationHint.Field("x", "X"), ObjectActivationHint.Field("y", "Y"))
                                                          .Map());

            StructSerializer.Map(ObjectSerializationMapper.ForType<ClassComposedOfStructs>().PublicFields().Map());

            StructSerializer.Map(ObjectSerializationMapper.ForType<Inner4>().PublicFields().Map());
            StructSerializer.Map(ObjectSerializationMapper.ForType<Inner3>().PublicFields().Map());
            StructSerializer.Map(ObjectSerializationMapper.ForType<Inner2>().PublicFields().Map());
            StructSerializer.Map(ObjectSerializationMapper.ForType<Inner1>().PublicFields().Map());
            StructSerializer.Map(ObjectSerializationMapper.ForType<Inner>().PublicFields().Map());
        }

        public StructSerializerTests()
        {
        }

        [Fact]
        public void Register_Throws_If_Type_Is_Already_Registered()
            => Assert.IsType<InvalidOperationException>(
                Record.Exception(() => StructSerializer.Map(ObjectSerializationMapper.ForType<Vec2>().Map()))
            );

        [Fact]
        public void Serializes_Objects_With_Indirect_Activation_Correctly_Back_And_Forth()
        {
            var testValueIn = new IndirectlyActivatedTestClass();

            StructSerializer.Map(ObjectSerializationMapper.ForType<IndirectlyActivatedTestClass>()
                                                          .PublicFields()
                                                          .IndirectActivation(() =>
                                                          {
                                                              return testValueIn;
                                                          })
                                                          .Map());

            testValueIn.X = testValueIn.Y = 256;

            var buffer = new byte[128];

            StructSerializer.Serialize(testValueIn, buffer, 0);

            var testValueOut = StructSerializer.Deserialize<IndirectlyActivatedTestClass>(buffer, 0);

            Assert.Same(testValueIn, testValueOut);
            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }

        [Fact]
        public void Serializes_Structures_Composed_Of_Structures_Back_And_Forth()
        {
            var testValueIn = new ClassComposedOfStructs
            {
                X = new Vec2(1.0f, 2.0f),
                Z = new Vec2(3.0f, 4.0f),
            };

            var buffer = new byte[128];

            StructSerializer.Serialize(testValueIn, buffer, 0);

            var testValueOut = StructSerializer.Deserialize<ClassComposedOfStructs>(buffer, 0);

            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }

        [Fact]
        public void Serializes_Complex_Nested_Structures_Composed_Of_Structures_Back_And_Forth()
        {
            var testValueIn = new Inner
            {
                Value = new Inner1
                {
                    Inner = new Inner2
                    {
                        Inner = new Inner3
                        {
                            Inner = new Inner4
                            {
                                Value = new Vec2(float.MinValue, float.MaxValue),
                            },
                        },
                    },
                },
            };

            var buffer = new byte[128];

            StructSerializer.Serialize(testValueIn, buffer, 0);

            var testValueOut = StructSerializer.Deserialize<Inner>(buffer, 0);

            Assert.Equal(JsonConvert.SerializeObject(testValueIn), JsonConvert.SerializeObject(testValueOut));
        }
    }
}