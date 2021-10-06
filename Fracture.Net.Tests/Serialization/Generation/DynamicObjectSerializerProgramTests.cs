using System.Collections.Generic;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Xunit;

#pragma warning disable 649

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

        public DynamicObjectSerializerProgramTests()
        {
        }
        
        [Fact]
        public void Constructor_Should_Throw_If_Program_Serializer_Counts_Differ()
        {
            var exception = Record.Exception(() => new ObjectSerializerProgram(
                    typeof(int),
                    new List<ISerializationOp> { new SerializeValueOp(new SerializationValue(typeof(Foo).GetField("X")), typeof(IntSerializer)) }.AsReadOnly(),
                    new List<ISerializationOp>().AsReadOnly())
            );
            
            Assert.NotNull(exception);
            Assert.Contains("different count of value serializers", exception.Message);
        }
    }
}