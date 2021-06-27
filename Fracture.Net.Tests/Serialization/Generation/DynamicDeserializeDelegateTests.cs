using System.Linq;
using Fracture.Common.Memory;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Xunit;

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicDeserializeDelegateTests
    {
        #region Test types
        private sealed class ValueTypeFieldTestClass
        {
            #region Fields
            public int X, Y;
            #endregion
        }
        
        private sealed class ValueTypePropertyTestClass
        {
            #region Properties
            public int X
            {
                get;
                set;
            }
            public int Y
            {
                get;
                set;
            }
            #endregion
        }
        
        private sealed class ValueTypePropertyAndFieldWithParametrizedConstructorTestClass
        {
            #region Fields
            public readonly int X;
            public readonly int Y;
            #endregion
            
            #region Properties
            public int X2
            {
                get;
            }

            public int Y2
            {
                get;
            }
            #endregion

            public ValueTypePropertyAndFieldWithParametrizedConstructorTestClass(int x, int y, int x2, int y2)
            {
                X2 = x2;
                Y2 = y2;
                
                X = x;
                Y = y;
            }
        }
        
        private sealed class NullableFieldTestClass
        {
            #region Fields
            public int? X;
            public int I;
            public int? Y;
            public int J;
            #endregion
        }
        
        private sealed class NullablePropertyTestClass
        {
            #region Properties
            public int? Y
            {
                get;
                set;
            }
            
            public int I
            {
                get;
                set;
            }
            
            public int? X
            {
                get;
                set;
            }
            
            public int J
            {
                get;
                set;
            }
            #endregion
        }

        private sealed class NonValueTypeFieldTestClass
        {
            #region Fields
            public int X, Y;
            
            public string S1;
            public string S2;

            public string? Null;
            #endregion    
        }
        
        private sealed class NonValueTypePropertyTestClass
        {
            #region Properties
            public int X
            {
                get;
                set;
            }
            
            public int Y
            {
                get;
                set;
            }
            
            public string S1
            {
                get;
                set;
            }            
            public string S2
            {
                get;
                set;
            }
            
            public string? Null
            {
                get;
                set;
            }
            #endregion    
        }
        #endregion
        
        [Fact]
        public void Should_Deserialize_Value_Type_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ValueTypeFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(ValueTypeFieldTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(ValueTypeFieldTestClass), 
                deserializationOps
            );
            
            var buffer = new byte[32];
            
            MemoryMapper.WriteInt(100, buffer, 0);
            MemoryMapper.WriteInt(200, buffer, sizeof(int));
            
            var results = (ValueTypeFieldTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(100, results.X);
            Assert.Equal(200, results.Y);
        }
        
        [Fact]
        public void Should_Deserialize_Value_Type_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ValueTypePropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(ValueTypePropertyTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(ValueTypePropertyTestClass), 
                deserializationOps
            );
            
            var buffer = new byte[32];
            
            MemoryMapper.WriteInt(300, buffer, 0);
            MemoryMapper.WriteInt(400, buffer, sizeof(int));
            
            var results = (ValueTypePropertyTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(300, results.X);
            Assert.Equal(400, results.Y);
        }
        
        [Fact]
        public void Should_Deserialize_Objects_Containing_Value_Type_Fields_And_Properties_With_Parametrized_Constructor()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ValueTypePropertyAndFieldWithParametrizedConstructorTestClass>()
                                                   .PublicProperties()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "X"),
                                                                           ObjectActivationHint.Field("y", "Y"),
                                                                           ObjectActivationHint.Property("x2", "X2"),
                                                                           ObjectActivationHint.Property("y2", "Y2"))
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(ValueTypePropertyAndFieldWithParametrizedConstructorTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(ValueTypePropertyAndFieldWithParametrizedConstructorTestClass), 
                deserializationOps
            );
            
            var buffer = new byte[32];
            
            MemoryMapper.WriteInt(10, buffer, 0);
            MemoryMapper.WriteInt(20, buffer, sizeof(int));
            MemoryMapper.WriteInt(30, buffer, sizeof(int) * 2);
            MemoryMapper.WriteInt(40, buffer, sizeof(int) * 3);
            
            var results = (ValueTypePropertyAndFieldWithParametrizedConstructorTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(10, results.X);
            Assert.Equal(20, results.Y);
            Assert.Equal(30, results.X2);
            Assert.Equal(40, results.Y2);
        }
        
        [Fact]
        public void Should_Deserialize_Nullable_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NullableFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(NullableFieldTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(NullableFieldTestClass), 
                deserializationOps
            );
            
            var bitFieldSerializer = new BitFieldSerializer();
            var nullMask           = new BitField(1);
            
            nullMask.SetBit(0, true);
            
            var buffer = new byte[32];
            var offset = 0;
            
            bitFieldSerializer.Serialize(nullMask, buffer, offset);
            offset += bitFieldSerializer.GetSizeFromValue(nullMask);
            
            MemoryMapper.WriteInt(25, buffer, offset);
            offset += sizeof(int);
                
            MemoryMapper.WriteInt(50, buffer, offset);
            offset += sizeof(int);

            MemoryMapper.WriteInt(75, buffer, offset);

            var results = (NullableFieldTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(25, results.I);
            Assert.Equal(50, results.Y);
            Assert.Equal(75, results.J);
        }
        
        [Fact]
        public void Should_Deserialize_Nullable_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NullablePropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(NullablePropertyTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(NullablePropertyTestClass), 
                deserializationOps
            );
            
            var bitFieldSerializer = new BitFieldSerializer();
            var nullMask           = new BitField(1);
            
            nullMask.SetBit(0, true);
            
            var buffer = new byte[32];
            var offset = 0;
            
            bitFieldSerializer.Serialize(nullMask, buffer, offset);
            offset += bitFieldSerializer.GetSizeFromValue(nullMask);
            
            MemoryMapper.WriteInt(50, buffer, offset);
            offset += sizeof(int);
                
            MemoryMapper.WriteInt(75, buffer, offset);
            offset += sizeof(int);

            MemoryMapper.WriteInt(100, buffer, offset);

            var results = (NullablePropertyTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(50, results.I);
            Assert.Equal(75, results.X);
            Assert.Equal(100, results.J);
        }
        
        [Fact]
        public void Should_Deserialize_Non_Value_Type_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NonValueTypeFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(NonValueTypeFieldTestClass),
                deserializationOps, 
                ObjectSerializerProgram.GetOpSerializers(deserializationOps).ToList().AsReadOnly()
            );
            
            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                context,
                typeof(NonValueTypeFieldTestClass), 
                deserializationOps
            );
            
            var stringSerializer   = new StringSerializer();
            var bitFieldSerializer = new BitFieldSerializer();
            var nullMask           = new BitField(1);
            
            nullMask.SetBit(0, true);
            
            var buffer = new byte[64];
            var offset = 0;
            
            bitFieldSerializer.Serialize(nullMask, buffer, offset);
            offset += bitFieldSerializer.GetSizeFromValue(nullMask);
            
            // X.
            MemoryMapper.WriteInt(25, buffer, offset);
            offset += sizeof(int);
                
            // Y.
            MemoryMapper.WriteInt(50, buffer, offset);
            offset += sizeof(int);
            
            // S2.
            stringSerializer.Serialize("hello!", buffer, offset);
            
            var results = (NonValueTypeFieldTestClass)deserializeDelegate(context, buffer, 0);
            
            Assert.Equal(25, results.X);
            Assert.Equal(50, results.Y);
            Assert.Equal("hello!", results.S2);
            
            Assert.Null(results.S1);
            
            Assert.True(string.IsNullOrEmpty(results.Null));
        }
    }
}