using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class PrimitiveSerializerTests
    {
        #region Data source
        private static class DataSource
        {
            #region Properties
            public static IEnumerable<object[]> Serializer_Ctor_Test_Data_Source => new List<object[]>()
            {
                new object[] { new Action(() => new SbyteSerializer()) },
                new object[] { new Action(() => new ByteSerializer()) },
                new object[] { new Action(() => new ShortSerializer()) },
                new object[] { new Action(() => new UshortSerializer()) },
                new object[] { new Action(() => new IntSerializer()) },
                new object[] { new Action(() => new UintSerializer()) },
                new object[] { new Action(() => new FloatSerializer()) },
                new object[] { new Action(() => new DoubleSerializer()) },
                new object[] { new Action(() => new StringSerializer()) },
            };
        
            public static IEnumerable<object[]> Serializes_To_Buffer_Correctly_Data_Source => new List<object[]>()
            {
                new object[] { new FloatSerializer(), 772842.0f, new byte[sizeof(float)], new byte[] { 160, 174, 60, 73 } }
            }; 
            
            public static IEnumerable<object[]> Deserializes_To_Value_Correctly_Data_Source => new List<object[]>()
            {
                new object[] { new FloatSerializer(), new byte[] { 160, 174, 60, 73 }, 772842.0f }
            };
            #endregion
        }
        #endregion
        
        #region Fields
        private IntSerializer serializer;
        #endregion
        
        [Theory()]
        [MemberData(nameof(DataSource.Serializer_Ctor_Test_Data_Source), MemberType = typeof(DataSource))]
        public void Serializer_Ctor_Test(Action construct)
        {
            Assert.Null(Record.Exception(construct));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Serializes_To_Buffer_Correctly_Data_Source), MemberType = typeof(DataSource))]
        public void Serializes_To_Buffer_Correctly(IValueSerializer serializer, 
                                                   object value, 
                                                   byte[] serializationBuffer, 
                                                   byte[] expectedSerializedBytes)
        {
            serializer.Serialize(value, serializationBuffer, 0);
            
            Assert.Equal(expectedSerializedBytes, serializationBuffer);
        }

        [Theory()]
        [MemberData(nameof(DataSource.Deserializes_To_Value_Correctly_Data_Source), MemberType = typeof(DataSource))]
        public void Deserializes_To_Value_Correctly(IValueSerializer serializer, 
                                                    byte[] serializedBytes, 
                                                    object expectedValue)
        {
            var value = serializer.Deserialize(serializedBytes, 0);
            
            Assert.Equal(expectedValue, value);
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