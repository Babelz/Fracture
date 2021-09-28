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
        public void Serializes_Primitive_Types_To_Buffer_Correctly()
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
            
            // Content length.
            Assert.Equal(21, Protocol.ContentLength.Read(buffer, offset));
            offset += Protocol.ContentLength.Size;
            
            // Collection length + omitted sparse collection flag.
            Assert.Equal(4, Protocol.CollectionLength.Read(buffer, offset));
            offset += Protocol.CollectionLength.Size + Protocol.TypeData.Size;
            
            // First element int.MinValue.
            Assert.Equal(int.MinValue, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(int.MinValue);
            
            // Second element 128.
            Assert.Equal(128, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(128);
            
            // Third element 256.
            Assert.Equal(256, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(256);

            // Last element int.MaxValue.
            Assert.Equal(int.MaxValue, IntSerializer.Deserialize(buffer, offset));
        }
        
        [Fact]
        public void Serializes_Primitive_Nullable_Types_To_Buffer_Correctly()
        {
            var numbers = new int?[8]
            {
                0,
                1,
                2,
                null,
                4,
                null,
                null,
                7
            };
            
            var buffer = new byte[128];
            
            ArraySerializer.Serialize(numbers, buffer, 0);
            
            var offset = 0;
            
            // Content length.
            Assert.Equal(21, Protocol.ContentLength.Read(buffer, offset));
            offset += Protocol.ContentLength.Size;
            
            // Collection length + omitted sparse collection flag.
            Assert.Equal(4, Protocol.CollectionLength.Read(buffer, offset));
            offset += Protocol.CollectionLength.Size + Protocol.TypeData.Size;
            
            // First element int.MinValue.
            Assert.Equal(int.MinValue, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(int.MinValue);
            
            // Second element 128.
            Assert.Equal(128, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(128);
            
            // Third element 256.
            Assert.Equal(256, IntSerializer.Deserialize(buffer, offset));
            offset += IntSerializer.GetSizeFromValue(256);

            // Last element int.MaxValue.
            Assert.Equal(int.MaxValue, IntSerializer.Deserialize(buffer, offset));
        }
    }
}