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
        #endregion
        
        [Fact]
        public void Should_Throw_If_Program_Types_Differ()
        {
            var exception = Record.Exception(() => ObjectSerializerInterpreter.InterpretSerializer(
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp>().AsReadOnly()),
                new ObjectSerializationProgram(typeof(float), new List<ISerializationOp>().AsReadOnly())
            ));
            
            Assert.NotNull(exception);
            Assert.Contains("are different", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Program_Serializer_Counts_Differ()
        {
            var exception = Record.Exception(() => ObjectSerializerInterpreter.InterpretSerializer(
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp> { new SerializationFieldOp(new SerializationValue(typeof(FieldTestClass).GetField("X"))) }.AsReadOnly()),
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp>().AsReadOnly())
            ));
            
            Assert.NotNull(exception);
            Assert.Contains("different count of value serializers", exception.Message);
        }
        
        [Fact]
        public void Should_Serialize_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<FieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializeProgram  = ObjectSerializerCompiler.CompileSerialize(mapping);
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(serializeProgram);
            var testObject        = new FieldTestClass() { X = 1500, Y = 37500 };
            var context           = new ObjectSerializationContext(new NullSerializer(), ObjectSerializerInterpreter.GetProgramSerializers(serializeProgram));
            var buffer            = new byte[8];
            
            serializeDelegate(context, testObject, buffer, 0);
            
            Assert.Equal(testObject.X, MemoryMapper.ReadInt(buffer, 0));
            Assert.Equal(testObject.Y, MemoryMapper.ReadInt(buffer, sizeof(int)));
        }
        
        [Fact]
        public void Should_Serialize_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<PropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializeProgram  = ObjectSerializerCompiler.CompileSerialize(mapping);
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(serializeProgram);
            var testObject        = new PropertyTestClass() { Id = 255255, Greet = "Hello stranger!" };
            var stringSerializer  = new StringSerializer();
            var context           = new ObjectSerializationContext(new NullSerializer(), ObjectSerializerInterpreter.GetProgramSerializers(serializeProgram));
            var buffer            = new byte[64];
            
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
            
            var serializeProgram  = ObjectSerializerCompiler.CompileSerialize(mapping);
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(serializeProgram);
            var testObject        = new FieldAndPropertyTestClass() { B1 = 0, B2 = 20, B3 = 150, B4 = 200 };
            var context           = new ObjectSerializationContext(new NullSerializer(), ObjectSerializerInterpreter.GetProgramSerializers(serializeProgram));
            var buffer            = new byte[64];
            
            serializeDelegate(context, testObject, buffer, 0);
            
            Assert.Equal(testObject.B1, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(testObject.B2, MemoryMapper.ReadByte(buffer, sizeof(byte)));
            Assert.Equal(testObject.B3, MemoryMapper.ReadByte(buffer, sizeof(byte) * 2));
            Assert.Equal(testObject.B4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 3));
        }
    }
}