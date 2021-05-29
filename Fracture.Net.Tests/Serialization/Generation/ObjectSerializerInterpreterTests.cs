using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Fracture.Common.Memory;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Xunit;

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicObjectSerializerProgramTests
    {
        #region Test types
        private sealed class Foo
        {
            #region Fields
            public int X;
            #endregion
        }
        #endregion
        
        [Fact]
        public void Constructor_Should_Throw_If_Program_Serializer_Counts_Differ()
        {
            var exception = Record.Exception(() => new ObjectSerializerProgram(
                typeof(int),
                new List<ISerializationOp> { new SerializationFieldOp(new SerializationValue(typeof(Foo).GetField("X"))) }.AsReadOnly(),
                new List<ISerializationOp>().AsReadOnly())
            );
            
            Assert.NotNull(exception);
            Assert.Contains("different count of value serializers", exception.Message);
        }
    }
    
    [Trait("Category", "Serialization")]
    public class ObjectSerializerInterpreterTests
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
        
        private sealed class FieldAndPropertyTestClass
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
        
        public sealed class NonValueTypeFieldsTest
        {
            public string S1;
            public string S2;
            public string S3;
            public int I;
            public int J;
        }
        #endregion

        [Fact]
        public void Should_Serialize_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<FieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps  = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(FieldTestClass), serializationOps, 0);
            var testObject        = new FieldTestClass() { X = 1500, Y = 37500 };
            var buffer            = new byte[8];

            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            serializeDelegate(context, testObject, buffer, 0);
            
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
            
            var serializationOps  = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(NullableFieldTestClass), serializationOps, 2);
            var testObject        = new NullableFieldTestClass() { X = null, Y = null, I = 200, J = 300 };
            var buffer            = new byte[64];

            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            serializeDelegate(context, testObject, buffer, 0);
            
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
                                                   .FromType<NonValueTypeFieldsTest>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps  = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(NonValueTypeFieldsTest), serializationOps, 2);
            var testObject        = new NonValueTypeFieldsTest() { S1 = "Hello fucking world", S2 = null, S3 = "Hello again", I = 1993, J = 200 };
            var buffer            = new byte[128];

            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            serializeDelegate(context, testObject, buffer, 0);
            
            // Null mask size in bytes.
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, 0));
            // Null mask values.
            Assert.Equal(160, MemoryMapper.ReadByte(buffer, sizeof(byte)));
        }
        
        [Fact]
        public void Should_Serialize_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<PropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializationOps  = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(PropertyTestClass), serializationOps, 0);
            var testObject        = new PropertyTestClass() { Id = 255255, Greet = "Hello stranger!" };
            var stringSerializer  = new StringSerializer();
            var buffer            = new byte[64];
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            serializeDelegate(context, testObject, buffer, 0);
            
            Assert.Equal(testObject.Id, MemoryMapper.ReadInt(buffer, 0));
            Assert.Equal(testObject.Greet, stringSerializer.Deserialize(buffer, sizeof(int)));
        }
        
        [Fact]
        public void Should_Serialize_Both_Properties_And_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<FieldAndPropertyTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps  = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(PropertyTestClass), serializationOps, 0);
            var testObject        = new FieldAndPropertyTestClass() { B1 = 0, B2 = 20, B3 = 150, B4 = 200 };
            var buffer            = new byte[64];
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            serializeDelegate(context, testObject, buffer, 0);
            
            Assert.Equal(testObject.B1, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(testObject.B2, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            Assert.Equal(testObject.B3, MemoryMapper.ReadByte(buffer, sizeof(byte) * 2));
            Assert.Equal(testObject.B4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 3));
        }
    }
}