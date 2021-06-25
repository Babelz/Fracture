using System.Linq;
using Fracture.Common.Memory;
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
    }
}