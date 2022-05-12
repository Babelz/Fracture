using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class StringSerializerTests 
    {
        public StringSerializerTests()
        {
        }
        
        [Fact]
        public void Serialized_String_Contains_Size_Of_The_Serialized_String()
        {
            var buffer = new byte[128];
            
            StringSerializer.Serialize("Hello!", buffer, 0);
            
            Assert.Equal(14, Protocol.ContentLength.Read(buffer, 0));
        }
        
        [Fact]
        public void Serializes_To_UTF_16_Format()
        {
            const string TestUnicodeString = "�ϿЀ"; 
            
            var buffer = new byte[128];
            
            StringSerializer.Serialize(TestUnicodeString, buffer, 0);
            
            Assert.Equal(TestUnicodeString, StringSerializer.Deserialize(buffer, 0));
        }
        
        [Fact]
        public void Get_Size_From_Value_Returns_String_Size_In_And_Size_Field_Bytes()
        {
            Assert.Equal(10, StringSerializer.GetSizeFromValue("hell"));
            Assert.Equal(14, StringSerializer.GetSizeFromValue("      "));
        }
        
        [Fact]
        public void Get_Size_From_Buffer_Returns_String_Size_And_Size_Field_In_Bytes()
        {
            Assert.Equal(10, StringSerializer.GetSizeFromBuffer(new byte[] { 10, 0, 0, 0 }, 0));
        }
        
        [Fact]
        public void Get_Size_From_Buffer_And_Get_Size_From_Value_Both_Return_Same_Value()
        {
            const string TestValue = "this is a long string with length of 90 bytes"; 
            
            var buffer = new byte[128];
            
            StringSerializer.Serialize(TestValue, buffer, 0);
            
            Assert.Equal(92, StringSerializer.GetSizeFromValue(TestValue));
            Assert.Equal(92, StringSerializer.GetSizeFromBuffer(buffer, 0));
        }
    }
}