using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            public float X;
            public float Y;
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
            Assert.Contains(exception.Message, "are different");
        }
        
        [Fact]
        public void Serializes_Fields_Properly()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<Vector2>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializeProgram = ObjectSerializerCompiler.CompileSerialize(mapping);
            
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(serializeProgram);
            
            
        }
    }
}