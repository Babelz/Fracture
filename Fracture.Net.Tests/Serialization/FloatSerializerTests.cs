using System;
using System.ComponentModel;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class FloatSerializerTests
    {
        #region Fields
        private readonly FloatSerializer serializer; 
        #endregion
        
        public FloatSerializerTests()
        {
            serializer = new FloatSerializer();
        }
        
        [Fact()]
        public void Float_Serializer_Ctor_Test()
        {
            Assert.Null(Record.Exception(() => new FloatSerializer()));
        }
        
        [Fact()]
        public void Serializes_Float_To_Four_Bytes()
        {
            const float Value = 772842.0f;
            
            var buffer = new byte[sizeof(float)];
            
            serializer.Serialize(Value, buffer, 0);
            
            Assert.Equal(160, buffer[0]);
            Assert.Equal(174, buffer[1]);
            Assert.Equal(60, buffer[2]);
            Assert.Equal(73, buffer[3]);
        }

        [Fact()]
        public void Deserializes_Four_Bytes_To_Float()
        {
            var value = (float)serializer.Deserialize(new byte[]
            {
                160, 174, 60, 73
            }, 0);
            
            Assert.Equal(772842.0f, value);
        }
        
        [Fact()]
        public void Size_From_Value_Is_Four()
        {
            Assert.Equal(4, serializer.GetSizeFromValue(2948.0f));
        }
        
        [Fact()]
        public void Size_From_Buffer_Is_Four()
        {
            Assert.Equal(4, serializer.GetSizeFromValue(new byte[] { 24, 22, 224, 11 }));
        }
        
        [Fact()]
        public void Serialize_Throws_On_Buffer_Overflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(1.0f, new byte[16], 16));
        }
        
        [Fact()]
        public void Deserialize_Throws_On_Buffer_Overflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[8], 7));
        }
        
        [Fact()]
        public void Serialize_Throws_On_Buffer_Underflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(1.0f, new byte[4], -5));
        }
        
        [Fact()]
        public void Deserialize_Throws_On_Buffer_Underflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[5], -9));
        }
    }
}