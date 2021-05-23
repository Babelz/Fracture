using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private sealed class Vector2
        {
            #region Fields
            public int X;
            public int Y;
            #endregion
        }
        
        private sealed class Dialog
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
        #endregion
        
        [Fact]
        public void Should_Throw_If_Program_Types_Differ()
        {
            var exception = Record.Exception(() => ObjectSerializerInterpreter.InterpretSerializer(
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp>().AsReadOnly()),
                new ObjectSerializationProgram(typeof(float), new List<ISerializationOp>().AsReadOnly())
            ));
            
            Assert.NotNull(exception);
            Assert.Contains(exception.Message, "are different");
        }
        
        [Fact]
        public void Should_Throw_If_Program_Serializer_Counts_Differ()
        {
            var exception = Record.Exception(() => ObjectSerializerInterpreter.InterpretSerializer(
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp> { new SerializationFieldOp(new SerializationValue(typeof(Vector2).GetField("X"))) }.AsReadOnly()),
                new ObjectSerializationProgram(typeof(int), new List<ISerializationOp>().AsReadOnly())
            ));
            
            Assert.NotNull(exception);
            Assert.Contains(exception.Message, "different count of value serializers");
        }
        
        [Fact]
        public void Serializes_Fields_Properly()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<Vector2>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializeProgram  = ObjectSerializerCompiler.CompileSerialize(mapping);
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(serializeProgram);
            var context           = new ObjectSerializationContext(new NullSerializer(), ObjectSerializerInterpreter.GetProgramSerializers(serializeProgram));
            var buffer            = new byte[8];
            
            serializeDelegate(context, new Vector2() { X = 1500, Y = 37500 }, buffer, 0);
            
            Assert.Equal(1500, MemoryMapper.ReadInt(buffer, 0));
            Assert.Equal(37500, MemoryMapper.ReadInt(buffer, sizeof(int)));
        }
    }
}