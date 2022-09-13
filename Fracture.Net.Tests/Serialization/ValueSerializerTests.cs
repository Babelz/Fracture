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
            public static IEnumerable<object []> SerializesToBufferCorrectlyDataSource
                => new []
                {
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(SbyteSerializer)).CreateDelegate(typeof(SerializeDelegate<sbyte>)),
                        (sbyte)-45, new byte[sizeof(sbyte)], new byte [] { 211 }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(ByteSerializer)).CreateDelegate(typeof(SerializeDelegate<byte>)),
                        (byte)128, new byte[sizeof(byte)], new byte [] { 128 }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(ShortSerializer)).CreateDelegate(typeof(SerializeDelegate<short>)),
                        (short)-7724, new byte[sizeof(ushort)], new byte [] { 212, 225 }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(UshortSerializer)).CreateDelegate(typeof(SerializeDelegate<ushort>)),
                        ushort.MaxValue, new byte[sizeof(short)], new byte [] { 255, 255 }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(IntSerializer)).CreateDelegate(typeof(SerializeDelegate<int>)),
                        -884752274,
                        new byte[sizeof(int)],
                        new byte []
                        {
                            110,
                            192,
                            67,
                            203
                        }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(UintSerializer)).CreateDelegate(typeof(SerializeDelegate<uint>)),
                        4294967295u,
                        new byte[sizeof(uint)],
                        Enumerable.Repeat((byte)255, 4).ToArray()
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(FloatSerializer)).CreateDelegate(typeof(SerializeDelegate<float>)),
                        772842.0f,
                        new byte[sizeof(float)],
                        new byte []
                        {
                            160,
                            174,
                            60,
                            73
                        }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(CharSerializer)).CreateDelegate(typeof(SerializeDelegate<char>)),
                        'C',
                        new byte[sizeof(char)],
                        new byte []
                        {
                            67,
                            0
                        }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(LongSerializer)).CreateDelegate(typeof(SerializeDelegate<long>)),
                        long.MaxValue,
                        new byte[sizeof(long)],
                        Enumerable.Repeat((byte)255, 7).Concat(new byte [] { 127 }).ToArray()
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(UlongSerializer)).CreateDelegate(typeof(SerializeDelegate<ulong>)),
                        ulong.MaxValue,
                        new byte[sizeof(ulong)],
                        Enumerable.Repeat((byte)255, 8).ToArray()
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(BoolSerializer)).CreateDelegate(typeof(SerializeDelegate<bool>)),
                        true,
                        new byte[sizeof(bool)],
                        new byte []
                        {
                            1
                        }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(BoolSerializer)).CreateDelegate(typeof(SerializeDelegate<bool>)),
                        false,
                        new byte[sizeof(bool)],
                        new byte []
                        {
                            0
                        }
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(TimeSpanSerializer)).CreateDelegate(typeof(SerializeDelegate<TimeSpan>)),
                        TimeSpan.MaxValue,
                        new byte[sizeof(long)],
                        Enumerable.Repeat((byte)255, 7).Concat(new byte [] { 127 }).ToArray()
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSerializeMethodInfo(typeof(DateTimeSerializer)).CreateDelegate(typeof(SerializeDelegate<DateTime>)),
                        DateTime.MaxValue,
                        new byte[sizeof(long)],
                        new byte []
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

            public static IEnumerable<object []> DeserializesToValueCorrectlyDataSource
                => new []
                {
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(SbyteSerializer)).CreateDelegate(typeof(DeserializeDelegate<sbyte>)),
                        new byte [] { 234 }, (sbyte)-22
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(ByteSerializer)).CreateDelegate(typeof(DeserializeDelegate<byte>)),
                        new byte [] { 128 }, (byte)128
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(ShortSerializer)).CreateDelegate(typeof(DeserializeDelegate<short>)),
                        new byte [] { 255, 127 }, short.MaxValue
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(UshortSerializer)).CreateDelegate(typeof(DeserializeDelegate<ushort>)),
                        new byte [] { 225, 212 }, (ushort)54497u
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(IntSerializer)).CreateDelegate(typeof(DeserializeDelegate<int>)),
                        new byte [] { 146, 63, 188, 52 }, 884752274
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(UintSerializer)).CreateDelegate(typeof(DeserializeDelegate<uint>)),
                        new byte [] { 52, 0, 0, 0 }, 52u
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(FloatSerializer)).CreateDelegate(typeof(DeserializeDelegate<float>)),
                        new byte [] { 160, 174, 60, 73 }, 772842.0f
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(CharSerializer)).CreateDelegate(typeof(DeserializeDelegate<char>)),
                        new byte [] { 62, 38 }, '☾'
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(LongSerializer)).CreateDelegate(typeof(DeserializeDelegate<long>)),
                        Enumerable.Repeat((byte)255, 7).Concat(new byte [] { 127 }).ToArray(), long.MaxValue
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(UlongSerializer)).CreateDelegate(typeof(DeserializeDelegate<ulong>)),
                        Enumerable.Repeat((byte)255, 8).ToArray(), ulong.MaxValue
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(BoolSerializer)).CreateDelegate(typeof(DeserializeDelegate<bool>)),
                        new byte [] { 1 }, true
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(TimeSpanSerializer))
                                               .CreateDelegate(typeof(DeserializeDelegate<TimeSpan>)),
                        Enumerable.Repeat((byte)255, 7).Concat(new byte [] { 127 }).ToArray(), TimeSpan.MaxValue
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(DateTimeSerializer))
                                               .CreateDelegate(typeof(DeserializeDelegate<DateTime>)),
                        new byte [] { 255, 63, 55, 244, 117, 40, 202, 43 },
                        DateTime.MaxValue
                    }
                };

            public static IEnumerable<object []> TestSizeFromValueDataSource
                => new []
                {
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(SbyteSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<sbyte>)),
                        (sbyte)-45, 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(ByteSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<byte>)),
                        (byte)255, 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(ShortSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<short>)),
                        (short)-9942, 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(UshortSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<ushort>)),
                        (ushort)7724u, 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(IntSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<int>)),
                        99482, 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(UintSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<uint>)),
                        42u, 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(FloatSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<float>)),
                        88283.0f, 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(CharSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<char>)),
                        'a', 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(LongSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<long>)),
                        (long)-299918, 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(UlongSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<ulong>)),
                        (ulong)88277u, 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(BoolSerializer)).CreateDelegate(typeof(GetSizeFromValueDelegate<bool>)),
                        true, 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(TimeSpanSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<TimeSpan>)),
                        TimeSpan.MaxValue, 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(DateTimeSerializer))
                                               .CreateDelegate(typeof(GetSizeFromValueDelegate<DateTime>)),
                        DateTime.MaxValue, 8
                    }
                };

            public static IEnumerable<object []> TestSizeFromBufferDataSource
                => new []
                {
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(SbyteSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 1).ToArray(), 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(ByteSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 1).ToArray(), 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(ShortSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 2).ToArray(), 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(UshortSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 2).ToArray(), 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(IntSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 4).ToArray(), 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(UintSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 4).ToArray(), 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(FloatSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 4).ToArray(), 4
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(CharSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 2).ToArray(), 2
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(LongSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 8).ToArray(), 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(UlongSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 8).ToArray(), 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(BoolSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 1).ToArray(), 1
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(TimeSpanSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 1).ToArray(), 8
                    },
                    new object []
                    {
                        ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(DateTimeSerializer)).CreateDelegate(typeof(GetSizeFromBufferDelegate)),
                        Enumerable.Repeat((byte)255, 1).ToArray(), 8
                    }
                };
            #endregion
        }
        #endregion

        public PrimitiveValueSerializerTests()
        {
        }

        [Theory]
        [MemberData(nameof(DataSource.SerializesToBufferCorrectlyDataSource), MemberType = typeof(DataSource))]
        public void Serializes_To_Buffer_Correctly(Delegate serializeDelegate,
                                                   object value,
                                                   byte [] serializationBuffer,
                                                   byte [] expectedSerializedBytes)
        {
            serializeDelegate.DynamicInvoke(value, serializationBuffer, 0);

            Assert.Equal(expectedSerializedBytes, serializationBuffer);
        }

        [Theory]
        [MemberData(nameof(DataSource.DeserializesToValueCorrectlyDataSource), MemberType = typeof(DataSource))]
        public void Deserializes_To_Value_Correctly(Delegate deserializeDelegate,
                                                    byte [] serializedBytes,
                                                    object expectedValue)
        {
            var value = deserializeDelegate.DynamicInvoke(serializedBytes, 0);

            Assert.Equal(expectedValue, value);
        }

        [Theory]
        [MemberData(nameof(DataSource.TestSizeFromValueDataSource), MemberType = typeof(DataSource))]
        public void Test_Size_From_Value(Delegate getSizeFromValueDelegate, object value, ushort expectedSize)
        {
            Assert.Equal(expectedSize, getSizeFromValueDelegate.DynamicInvoke(value));
        }

        [Theory]
        [MemberData(nameof(DataSource.TestSizeFromBufferDataSource), MemberType = typeof(DataSource))]
        public void Test_Size_From_Buffer(Delegate getSizeFromBufferDelegate, byte [] serializedBytes, ushort expectedSize)
        {
            Assert.Equal(expectedSize, getSizeFromBufferDelegate.DynamicInvoke(serializedBytes, 0));
        }
    }
}