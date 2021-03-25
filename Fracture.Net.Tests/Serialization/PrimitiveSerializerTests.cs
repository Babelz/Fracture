using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            public static IEnumerable<object[]> Serializer_Ctor_Test_Data_Source => new []
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
            
            public static IEnumerable<object[]> Test_Serializers_Source => new []
            {
                new object[] { new SbyteSerializer() },
                new object[] { new ByteSerializer() },
                new object[] { new ShortSerializer() },
                new object[] { new UshortSerializer() },
                new object[] { new IntSerializer() },
                new object[] { new UintSerializer() },
                new object[] { new FloatSerializer() },
                new object[] { new DoubleSerializer() },
                new object[] { new StringSerializer() } 
            };
        
            public static IEnumerable<object[]> Serializes_To_Buffer_Correctly_Data_Source => new []
            {
                new object[] { new FloatSerializer(), 772842.0f, new byte[sizeof(float)], new byte[] { 160, 174, 60, 73 } },
                new object[] { new IntSerializer(), -884752274, new byte[sizeof(int)], new byte[] { 110, 192, 67, 203, } }, 
            }; 
            
            public static IEnumerable<object[]> Deserializes_To_Value_Correctly_Data_Source => new []
            {
                new object[] { new FloatSerializer(), new byte[] { 160, 174, 60, 73 }, 772842.0f },
                new object[] { new IntSerializer(), new byte[] { 146, 63, 188, 52 }, 884752274}, 
            };
            
            public static IEnumerable<object[]> Test_Size_From_Value_Data_Source => new []
            {
                new object[] { new FloatSerializer(), 88283.0f, 4 },
                new object[] { new IntSerializer(), 99482, 4 },
            };
            
            public static IEnumerable<object[]> Test_Size_From_Buffer_Data_Source => new []
            {
                new object[] { new FloatSerializer(), new byte[] { 24, 22, 224, 11 }, 4 },
                new object[] { new IntSerializer(), new byte[] { 22, 14, 99, 2 }, 4 },
            };
            #endregion
        }
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
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Size_From_Value_Data_Source), MemberType = typeof(DataSource))]
        public void Test_Size_From_Value(IValueSerializer serializer, object value, ushort expectedSize)
        {
            Assert.Equal(expectedSize, serializer.GetSizeFromValue(value));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Size_From_Buffer_Data_Source), MemberType = typeof(DataSource))]
        public void Test_Size_From_Buffer(IValueSerializer serializer, byte[] serializedBytes, ushort expectedSize)
        {
            Assert.Equal(expectedSize, serializer.GetSizeFromBuffer(serializedBytes, 0));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Serializers_Source), MemberType = typeof(DataSource))]
        public void Serialize_Throws_On_Buffer_Overflow(IValueSerializer serializer)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(null, new byte[16], 32));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Serializers_Source), MemberType = typeof(DataSource))]
        public void Deserialize_Throws_On_Buffer_Overflow(IValueSerializer serializer)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[8], 16));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Serializers_Source), MemberType = typeof(DataSource))]
        public void Serialize_Throws_On_Buffer_Underflow(IValueSerializer serializer)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Serialize(null, new byte[4], -20));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.Test_Serializers_Source), MemberType = typeof(DataSource))]
        public void Deserialize_Throws_On_Buffer_Underflow(IValueSerializer serializer)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => serializer.Deserialize(new byte[5], -15));
        }
    }
}