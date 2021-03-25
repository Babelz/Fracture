using System;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class UintSerializerTests
    {
        #region Fields
        private readonly UintSerializer serializer; 
        #endregion
        
        public UintSerializerTests()
        {
            serializer = new UintSerializer();
        }
        
        [Fact()]
        public void Uint_Serializer_Ctor_Test()
        {
            Assert.Null(Record.Exception(() => new UintSerializer()));
        }
        
        [Fact()]
        public void Serializes_Uint_To_Four_Bytes()
        {
            const uint Value = 4294967295u;
            
            var buffer = new byte[sizeof(uint)];
            
            serializer.Serialize(Value, buffer, 0);
            
            Assert.Equal(255, buffer[0]);
            Assert.Equal(255, buffer[1]);
            Assert.Equal(255, buffer[2]);
            Assert.Equal(255, buffer[3]);
        }

        [Fact()]
        public void Deserializes_Four_Bytes_To_Int()
        {
            var value = (uint)serializer.Deserialize(new byte[]
            {
                52, 0, 0, 0
            }, 0);
            
            Assert.Equal(52u, value);
        }
        
        [Fact()]
        public void Size_From_Value_Is_Four()
        {
            Assert.Equal(4, serializer.GetSizeFromValue(44252u));
        }
        
        [Fact()]
        public void Size_From_Buffer_Is_Four()
        {
            Assert.Equal(4, serializer.GetSizeFromValue(new byte[] { 24, 22, 224, 11 }));
        }
        
        [Fact()]
        public void Serialize_Throws_On_Buffer_Overflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(55523213u, new byte[32], 30));
        }
        
        [Fact()]
        public void Deserialize_Throws_On_Buffer_Overflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[4], 3));
        }
        
        [Fact()]
        public void Serialize_Throws_On_Buffer_Underflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(225u, new byte[10], -11));
        }
        
        [Fact()]
        public void Deserialize_Throws_On_Buffer_Underflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[4], -9));
        }
    }
}