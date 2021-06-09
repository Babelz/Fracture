using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class PrimitiveValueSerializerTests
    {
        #region Data source
        private static class DataSource
        {
            #region Properties
            public static IEnumerable<object[]> SerializerCtorTestDataSource => new []
            {
                new object[] { new Action(() => new SbyteSerializer()) },
                new object[] { new Action(() => new ByteSerializer()) },
                new object[] { new Action(() => new ShortSerializer()) },
                new object[] { new Action(() => new UshortSerializer()) },
                new object[] { new Action(() => new IntSerializer()) },
                new object[] { new Action(() => new UintSerializer()) },
                new object[] { new Action(() => new FloatSerializer()) },
                new object[] { new Action(() => new CharSerializer()) },
                new object[] { new Action(() => new LongSerializer()) },
                new object[] { new Action(() => new UlongSerializer()) },
                new object[] { new Action(() => new TimeSpanSerializer()) },
                new object[] { new Action(() => new DateTimeSerializer()) }
            };

            public static IEnumerable<object[]> SerializesToBufferCorrectlyDataSource => new []
            {
                new object[] { new SbyteSerializer(), (sbyte)-45, new byte[sizeof(sbyte)], new byte[] { 211 } },
                new object[] { new ByteSerializer(), (byte)128, new byte[sizeof(byte)], new byte[] { 128 } },
                new object[] { new ShortSerializer(), (short)-7724, new byte[sizeof(ushort)], new byte[] { 212, 225 } },
                new object[] { new UshortSerializer(), ushort.MaxValue, new byte[sizeof(short)], new byte[] { 255, 255 } },
                new object[]
                {
                    new IntSerializer(),
                    -884752274,
                    new byte[sizeof(int)],
                    new byte[]
                    {
                        110,
                        192,
                        67,
                        203,
                    }
                }, 
                new object[]
                {
                    new UintSerializer(),
                    4294967295u,
                    new byte[sizeof(uint)],
                    Enumerable.Repeat((byte)255, 4).ToArray()
                },
                new object[]
                {
                    new FloatSerializer(),
                    772842.0f,
                    new byte[sizeof(float)],
                    new byte[]
                    {
                        160,
                        174,
                        60,
                        73
                    }
                },
                new object[] 
                { 
                    new CharSerializer(),
                    'C',
                    new byte[sizeof(char)],
                    new byte[]
                    {
                        67,
                        0
                    }
                },
                new object[] 
                { 
                    new LongSerializer(),
                    long.MaxValue,
                    new byte[sizeof(long)],
                    Enumerable.Repeat((byte)255, 7).Concat(new byte[] { 127 }).ToArray()
                },
                new object[] 
                { 
                    new UlongSerializer(),
                    ulong.MaxValue,
                    new byte[sizeof(ulong)],
                    Enumerable.Repeat((byte)255, 8).ToArray()
                },
                new object[]
                {
                    new BoolSerializer(),
                    true,
                    new byte[sizeof(bool)],
                    new byte[]
                    {
                        1
                    }
                },
                new object[]
                {
                    new BoolSerializer(),
                    false,
                    new byte[sizeof(bool)],
                    new byte[]
                    {
                        0
                    }
                },
                new object[]
                {
                    new TimeSpanSerializer(),
                    TimeSpan.MaxValue,
                    new byte[sizeof(long)],
                    Enumerable.Repeat((byte)255, 7).Concat(new byte[] { 127 }).ToArray(),
                },
                new object[]
                {
                    new DateTimeSerializer(),
                    DateTime.MaxValue,
                    new byte[sizeof(long)],
                    new byte[]
                    {
                        255,
                        63,
                        55,
                        244,
                        117,
                        40,
                        202,
                        43
                    }
                }
            }; 
            
            public static IEnumerable<object[]> DeserializesToValueCorrectlyDataSource => new []
            {
                new object[] { new SbyteSerializer(), new byte[] { 234 }, (sbyte)-22 },
                new object[] { new ByteSerializer(), new byte[] { 128 }, (byte)128 },
                new object[] { new ShortSerializer(), new byte[] { 255, 127 }, short.MaxValue },
                new object[] { new UshortSerializer(), new byte[] { 225, 212 }, (ushort)54497u },
                new object[] { new IntSerializer(), new byte[] { 146, 63, 188, 52 }, 884752274 },
                new object[] { new UintSerializer(), new byte[] { 52, 0, 0, 0 }, 52u }, 
                new object[] { new FloatSerializer(), new byte[] { 160, 174, 60, 73 }, 772842.0f },
                new object[] { new CharSerializer(), new byte[] { 62, 38 }, '☾' },
                new object[] { new LongSerializer(), Enumerable.Repeat((byte)255, 7).Concat(new byte[] { 127 }).ToArray(), long.MaxValue },
                new object[] { new UlongSerializer(), Enumerable.Repeat((byte)255, 8).ToArray(), ulong.MaxValue },
                new object[] { new BoolSerializer(), new byte[] { 1 }, true },
                new object[] { new BoolSerializer(), new byte[] { 0 }, false },
                new object[] { new TimeSpanSerializer(), Enumerable.Repeat((byte)255, 7).Concat(new byte[] { 127 }).ToArray(), TimeSpan.MaxValue },
                new object[] { new DateTimeSerializer(), new byte[] { 255, 63, 55, 244, 117, 40, 202, 43 }, DateTime.MaxValue }
            };
            
            public static IEnumerable<object[]> TestSizeFromValueDataSource => new []
            {
                new object[] { new SbyteSerializer(), (sbyte)-45, 1 },
                new object[] { new ByteSerializer(), (byte)255, 1 },
                new object[] { new ShortSerializer(), (short)-9942, 2 },
                new object[] { new UshortSerializer(), (ushort)7724u, 2 },
                new object[] { new IntSerializer(), 99482, 4 },
                new object[] { new UintSerializer(), 42u, 4 },
                new object[] { new FloatSerializer(), 88283.0f, 4 },
                new object[] { new CharSerializer(), 'a', 2 },
                new object[] { new LongSerializer(), (long)-299918, 8, },
                new object[] { new UlongSerializer(), (ulong)88277u, 8 },
                new object[] { new BoolSerializer(), true, 1 },
                new object[] { new TimeSpanSerializer(), TimeSpan.MaxValue, 8 },
                new object[] { new DateTimeSerializer(), DateTime.MaxValue, 8 }
            };
            
            public static IEnumerable<object[]> TestSizeFromBufferDataSource => new []
            {
                new object[] { new SbyteSerializer(), Enumerable.Repeat((byte)255, 1).ToArray(), 1 },
                new object[] { new ByteSerializer(), Enumerable.Repeat((byte)255, 1).ToArray(), 1 },
                new object[] { new ShortSerializer(), Enumerable.Repeat((byte)255, 2).ToArray(), 2 },
                new object[] { new UshortSerializer(), Enumerable.Repeat((byte)255, 2).ToArray(), 2 },
                new object[] { new IntSerializer(), Enumerable.Repeat((byte)255, 4).ToArray(), 4 },
                new object[] { new UintSerializer(), Enumerable.Repeat((byte)255, 4).ToArray(), 4 },
                new object[] { new FloatSerializer(), Enumerable.Repeat((byte)255, 4).ToArray(), 4 },
                new object[] { new CharSerializer(), Enumerable.Repeat((byte)255, 2).ToArray(), 2 },
                new object[] { new LongSerializer(), Enumerable.Repeat((byte)255, 8).ToArray(), 8 },
                new object[] { new UlongSerializer(), Enumerable.Repeat((byte)255, 8).ToArray(), 8 },
                new object[] { new BoolSerializer(), Enumerable.Repeat((byte)255, 1).ToArray(), 1 },
                new object[] { new TimeSpanSerializer(), Enumerable.Repeat((byte)255, 1).ToArray(), 8 },
                new object[] { new DateTimeSerializer(), Enumerable.Repeat((byte)255, 1).ToArray(), 8 }
            };
            #endregion
        }
        #endregion

        public PrimitiveValueSerializerTests()
        {
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.SerializerCtorTestDataSource), MemberType = typeof(DataSource))]
        public void Serializer_Ctor_Test(Action construct)
        {
            Assert.Null(Record.Exception(construct));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.SerializesToBufferCorrectlyDataSource), MemberType = typeof(DataSource))]
        public void Serializes_To_Buffer_Correctly(IValueSerializer serializer, 
                                                   object value, 
                                                   byte[] serializationBuffer, 
                                                   byte[] expectedSerializedBytes)
        {
            serializer.Serialize(value, serializationBuffer, 0);
            
            Assert.Equal(expectedSerializedBytes, serializationBuffer);
        }

        [Theory()]
        [MemberData(nameof(DataSource.DeserializesToValueCorrectlyDataSource), MemberType = typeof(DataSource))]
        public void Deserializes_To_Value_Correctly(IValueSerializer serializer, 
                                                    byte[] serializedBytes, 
                                                    object expectedValue)
        {
            var value = serializer.Deserialize(serializedBytes, 0);
            
            Assert.Equal(expectedValue, value);
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.TestSizeFromValueDataSource), MemberType = typeof(DataSource))]
        public void Test_Size_From_Value(IValueSerializer serializer, object value, ushort expectedSize)
        {
            Assert.Equal(expectedSize, serializer.GetSizeFromValue(value));
        }
        
        [Theory()]
        [MemberData(nameof(DataSource.TestSizeFromBufferDataSource), MemberType = typeof(DataSource))]
        public void Test_Size_From_Buffer(IValueSerializer serializer, byte[] serializedBytes, ushort expectedSize)
        {
            Assert.Equal(expectedSize, serializer.GetSizeFromBuffer(serializedBytes, 0));
        }
    }
}