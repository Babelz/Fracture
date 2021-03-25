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
        
        public void Serialized_String_Contains_Size_Of_The_String()
        {
            
        }
        
        public void Serializes_To_UTF_16_Format()
        {
        }
        
        public void Get_Size_From_Value_Returns_String_Size_In_Bytes()
        {
        }
        
        public void Get_Size_From_Buffer_Returns_String_Size_And_Size_Field_In_Bytes()
        {
        }
    }
}