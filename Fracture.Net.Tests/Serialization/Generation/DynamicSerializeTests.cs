using System.Linq;
using Fracture.Common.Memory;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Xunit;

#pragma warning disable 8632

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicSerializeTests
    {
        #region Test types
        private sealed class FieldTestClass
        {
            #region Fields
            public int X;
            public int Y;
            #endregion
        }
        
        private sealed class NullableFieldTestClass
        {
            #region Fields
            public int? X;
            public int? Y;

            public int I;
            public int J;
            #endregion
        }
        
        private sealed class NullablePropertyTestClass
        {
            #region Fields
            public int? X
            {
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                get;
                set;
            }
            public int? Y
            {
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                get;
                set;
            }

            public int I
            {
                get;
                set;
            }
            public int J
            {
                get;
                set;
            }
            
            public string S1
            {
                get;
                set;
            }
            #endregion
        }
        
        private sealed class PropertyTestClass
        {
            #region Properties
            public int Id
            {
                get;
                set;
            }
            
            public string Greet
            {
                get;
                set;
            }
            #endregion
        }
        
        private sealed class FieldAndPropertyTestClassClass
        {
            #region Properties
            public byte B1
            {
                get;
                set;
            }
            public byte B2
            {
                get;
                set;
            }
            #endregion

            #region Fields
            public byte B3;
            public byte B4;
            #endregion
        }

        private sealed class NonValueTypeFieldTestClass
        {
            #region Fields
            public string S1;
            public string S2;
            public string S3;
            public int I;
            public int J;
            #endregion
        }

        private sealed class NonValueTypePropertyTestClass
        {
            #region Properties
            public string S1
            {
                get;
                set;
            }
            public string S2
            {
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                get;
                set;
            }
            public string S3
            {
                get;
                set;
            }
            public int I
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

        private sealed class AllPropertyAndFieldKindsMixTestClass
        {
            #region Fields
            public int? X;
            public int Y;
            
            public string? S1;
            public string S2;
            #endregion

            #region Properties
            public int? I
            {
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                get;
                set;
            }
            public int J
            {
                get;
                set;
            }
            
            public string? S3
            {
                // ReSharper disable once UnusedAutoPropertyAccessor.Local
                get;
                set;
            }
            public string S4
            {
                get;
                set;
            }
            #endregion
        }

        private sealed class NonZeroNullValueOffsetMixedTestClass
        {
            #region Fields
            public int X;
            public int Y;
            public int I;
            public int J;
            #endregion

            #region Properties
            public int? V
            {
                get;
                set;
            }
            
            public int K
            {
                get;
                set;
            }
            
            public int? P1
            {
                get;
                set;
            }
            
            public int? P2
            {
                get;
                set;
            }
            
            public int? P3
            {
                get;
                set;
            }
            
            public int? P4
            {
                get;
                set;
            }
            
            public int? P5
            {
                get;
                set;
            }
            public int? P6
            {
                get;
                set;
            }
            public int? P7
            {
                get;
                set;
            }
            public int? P8
            {
                get;
                set;
            }
            
            public int P9
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        [Fact]
        public void Should_Serialize_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<FieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(FieldTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(FieldTestClass), 
                serializationOps
            );
            
            var testObject = new FieldTestClass() { X = 1500, Y = 37500 };
            var buffer     = new byte[8];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            Assert.Equal(testObject.X, MemoryMapper.ReadInt(buffer, 0));
            Assert.Equal(testObject.Y, MemoryMapper.ReadInt(buffer, sizeof(int)));
        }
        
        [Fact]
        public void Should_Serialize_Nullable_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NullableFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NullableFieldTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(NullableFieldTestClass), 
                serializationOps
            );
            
            var testObject = new NullableFieldTestClass() { X = null, Y = null, I = 200, J = 300 };
            var buffer     = new byte[64];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, 0));
            // Null mask values.
            Assert.Equal(192, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            // Field 'I' value.
            Assert.Equal(testObject.I, MemoryMapper.ReadInt(buffer, sizeof(byte) * 2));
            // Field 'J' value.
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, sizeof(int) + sizeof(byte) * 2));
        }
        
        [Fact]
        public void Should_Serialize_Non_Value_Type_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NonValueTypeFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NonValueTypeFieldTestClass), serializationOps);

            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(NonValueTypeFieldTestClass), 
                serializationOps
            );
            
            var testObject = new NonValueTypeFieldTestClass() { S1 = "Hello fucking world", S2 = null, S3 = "Hello again", I = 1993, J = 200 };
            var buffer     = new byte[128];
            
            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            var offset = 0;
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            // Null mask values.
            Assert.Equal(64, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            Assert.Equal(testObject.S1, StringSerializer.Deserialize(buffer, offset));
            offset += StringSerializer.GetSizeFromValue(testObject.S1);
            
            Assert.Equal(testObject.S3, StringSerializer.Deserialize(buffer, offset));
            offset += StringSerializer.GetSizeFromValue(testObject.S3);
            
            Assert.Equal(testObject.I, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, offset));
        }
        
        [Fact]
        public void Should_Serialize_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<PropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(PropertyTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(PropertyTestClass), 
                serializationOps
            );
            
            var testObject = new PropertyTestClass() { Id = 255255, Greet = "Hello stranger!" };
            var buffer     = new byte[64];
            
            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, 0));
            // Null mask values.
            Assert.Equal(0, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            
            Assert.Equal(testObject.Id, MemoryMapper.ReadInt(buffer, sizeof(byte) * 2));
            Assert.Equal(testObject.Greet, StringSerializer.Deserialize(buffer, sizeof(byte) * 2 + sizeof(int)));
        }
        
        [Fact]
        public void Should_Serialize_Nullable_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NullablePropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NullablePropertyTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(NullablePropertyTestClass), 
                serializationOps
            );
            
            var testObject = new NullablePropertyTestClass() { X = null, Y = null, I = 200, J = 300 };
            var buffer     = new byte[64];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, 0));
            // Null mask values.
            Assert.Equal(224, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            // Field 'I' value.
            Assert.Equal(testObject.I, MemoryMapper.ReadInt(buffer, sizeof(byte) * 2));
            // Field 'J' value.
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, sizeof(int) + sizeof(byte) * 2));
        }
        
        [Fact]
        public void Should_Serialize_Non_Value_Type_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NonValueTypePropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NonValueTypePropertyTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(NonValueTypePropertyTestClass), 
                serializationOps
            );
            
            var testObject = new NonValueTypePropertyTestClass() { S1 = "Hello fucking world", S2 = null, S3 = "Hello again", I = 1993, J = 200 };
            var buffer     = new byte[128];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            var offset = 0;
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            // Null mask values.
            Assert.Equal(64, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            Assert.Equal(testObject.S1, StringSerializer.Deserialize(buffer, offset));
            offset += StringSerializer.GetSizeFromValue(testObject.S1);
            
            Assert.Equal(testObject.S3, StringSerializer.Deserialize(buffer, offset));
            offset += StringSerializer.GetSizeFromValue(testObject.S3);
            
            Assert.Equal(testObject.I, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, offset));
        }
        
        [Fact]
        public void Should_Serialize_Both_Properties_And_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<FieldAndPropertyTestClassClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(FieldAndPropertyTestClassClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(FieldAndPropertyTestClassClass), 
                serializationOps
            );
            
            var testObject = new FieldAndPropertyTestClassClass() { B1 = 0, B2 = 20, B3 = 150, B4 = 200 };
            var buffer     = new byte[64];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            Assert.Equal(testObject.B1, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(testObject.B2, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            Assert.Equal(testObject.B3, MemoryMapper.ReadByte(buffer, sizeof(byte) * 2));
            Assert.Equal(testObject.B4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 3));
        }
        
        [Fact]
        public void Should_Serialize_Non_Zero_Null_Offset_Mixed_Value_Types()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NonZeroNullValueOffsetMixedTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            
            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NonZeroNullValueOffsetMixedTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(NonZeroNullValueOffsetMixedTestClass), 
                serializationOps
            );

            var testObject = new NonZeroNullValueOffsetMixedTestClass() { I = 200, J = 300, K = 400, X = 2, Y = 1, P9 = 500};
            var buffer     = new byte[64];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            var offset = 0;
            
            // Null mask size in bytes.
            Assert.Equal(2, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            // Null mask values.
            Assert.Equal(33023, MemoryMapper.ReadUshort(buffer, offset));
            offset += sizeof(ushort);
            
            // K.
            Assert.Equal(testObject.K, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // P9.
            Assert.Equal(testObject.P9, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // X.
            Assert.Equal(testObject.X, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // Y.
            Assert.Equal(testObject.Y, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // I.
            Assert.Equal(testObject.I, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // J.
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, offset));
        }
        
        [Fact]
        public void Should_Serialize_All_Supported_Field_And_Property_Kinds_Mixed()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<AllPropertyAndFieldKindsMixTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(AllPropertyAndFieldKindsMixTestClass), serializationOps);
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                valueRanges,
                typeof(AllPropertyAndFieldKindsMixTestClass), 
                serializationOps
            );
            
            var testObject = new AllPropertyAndFieldKindsMixTestClass() 
            {
                Y  = 200,
                X  = null,
                S2 = "hello world!",
                S1 = null,
                J  = 400,
                I  = null,
                S3 = null,
                S4 = "fuck you"
            };
            
            var buffer = new byte[256];

            serializeDelegate(valueRanges, testObject, buffer, 0);
            
            var offset = 0;
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            // Null mask values.
            Assert.Equal(216, MemoryMapper.ReadByte(buffer, offset));
            offset += sizeof(byte);
            
            // J.
            Assert.Equal(testObject.J, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // S4.
            Assert.Equal(testObject.S4, StringSerializer.Deserialize(buffer, offset));
            offset += StringSerializer.GetSizeFromBuffer(buffer, offset);
            
            // Y.
            Assert.Equal(testObject.Y, MemoryMapper.ReadInt(buffer, offset));
            offset += sizeof(int);
            
            // S2.
            Assert.Equal(testObject.S2, StringSerializer.Deserialize(buffer, offset));
        }
    }
}