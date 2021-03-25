using System.Net.Sockets;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class StringSerializerTests 
    {
        #region Fields
        private readonly StringSerializer serializer;
        #endregion
        
        public StringSerializerTests()
        {
            serializer = new StringSerializer();
        }
        
        [Fact()]
        public void Serialized_String_Contains_Size_Of_The_String()
        {
            var buffer = new byte[128];
            
            serializer.Serialize("Hello!", buffer, 0);
            
            Assert.Equal(12, Protocol.Message.Field.DynamicTypeLength.Read(buffer, 0));
        }
        
        [Fact()]
        public void Serializes_To_UTF_16_Format()
        {
            const string TestUnicodeString = "�ϿЀ"; 
            
            var buffer = new byte[128];
            
            serializer.Serialize(TestUnicodeString, buffer, 0);
            
            Assert.Equal(TestUnicodeString, serializer.Deserialize(buffer, 0));
        }
        
        [Fact()]
        public void Get_Size_From_Value_Returns_String_Size_In_Bytes()
        {
        }
        
        [Fact()]
        public void Get_Size_From_Buffer_Returns_String_Size_And_Size_Field_In_Bytes()
        {
        }
    }
}