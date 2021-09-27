using System.Reflection;
using Fracture.Net.Serialization;
using Iced.Intel;
using Xunit;
using Xunit.Sdk;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class ArraySerializerTests
    {
        [Fact]
        public void Serialize_Throws_If_Array_Type_Is_Unsupported()
            => Assert.NotNull(Record.Exception(() => ArraySerializer.Serialize(new Assembly[1], new byte[1], 0)));
        
        [Fact]
        public void Deserialize_Throws_If_Array_Type_Is_Unsupported()
            => Assert.NotNull(Record.Exception(() => ArraySerializer.Deserialize<Assembly>(new byte[1], 0)));
        
        [Fact]
        public void Get_Size_From_Value_Throws_If_Array_Type_Is_Unsupported()
            => Assert.NotNull(Record.Exception(() => ArraySerializer.GetSizeFromValue(new Assembly[1])));

        [Fact]
        public void Serializes_Primitive_Types_Back_And_Forth_Correctly()
        {
            var numbers = new int[4]
            {
                int.MinValue,
                128,
                256,
                int.MaxValue
            };
            
            var buffer = new byte[128];
            
            ArraySerializer.Serialize(numbers, buffer, 0);
            
            var offset = 0;
            
            Assert.Equal(20, Protocol.ContentLength.Read(buffer, offset));
            offset += Protocol.ContentLength.Size;
            
            Assert.Equal(4, Protocol.CollectionLength.Read(buffer, offset));
            offset += Protocol.CollectionLength.Size;
            
            Assert.Equal(int.MinValue, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(int.MinValue);
            
            Assert.Equal(128, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(128);
            
            Assert.Equal(256, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(256);

            Assert.Equal(int.MaxValue, IntSerializer.Deserialize(buffer, offset));
        }
    }
}