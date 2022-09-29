using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public class BitFieldTests
    {
        [Fact]
        public void Writes_To_Correct_Byte_Index()
        {
            var bf = new BitField(16);

            bf.SetBit(8 * 8, true);
            bf.SetBit(2, true);
            bf.SetBit(8 * 7 + 1, true);

            Assert.Equal(128, bf.GetByteAtIndex(8));
            Assert.Equal(32, bf.GetByteAtIndex(0));
            Assert.Equal(64, bf.GetByteAtIndex(7));
        }

        [Fact]
        public void Writes_Bit_To_Correct_Index()
        {
            var bf = new BitField(16);

            bf.SetBit(8 * 8, true);
            bf.SetBit(32, true);
            bf.SetBit(75, true);
            bf.SetBit(0, true);
            bf.SetBit(1, true);

            Assert.True(bf.GetBit(8 * 8));
            Assert.True(bf.GetBit(32));
            Assert.True(bf.GetBit(75));
            Assert.True(bf.GetBit(0));
            Assert.True(bf.GetBit(1));
        }

        [Fact]
        public void Handles_Bits_Past_64()
        {
            // 8192-bit field.
            var bf = new BitField(1024);

            bf.SetBit(512 * 8, true);

            Assert.True(bf.GetBit(512 * 8));
        }

        [Fact]
        public void Bits_Are_Ordered_From_Left_To_Right()
        {
            var bf = new BitField(4);

            bf.SetBit(0, true);
            bf.SetBit(1, true);

            bf.SetBit(14, true);
            bf.SetBit(15, true);

            Assert.Equal(192, bf.GetByteAtIndex(0));
            Assert.Equal(3, bf.GetByteAtIndex(1));
            Assert.Equal(0, bf.GetByteAtIndex(2));
            Assert.Equal(0, bf.GetByteAtIndex(3));
        }
    }

    [Trait("Category", "Serialization")]
    public class BitFieldSerializerTests
    {
        public BitFieldSerializerTests()
        {
        }

        [Fact]
        public void Serializes_To_Buffer_Correctly()
        {
            var bf = new BitField(4);

            bf.SetBit(7, true);
            bf.SetBit(15, true);
            bf.SetBit(23, true);
            bf.SetBit(31, true);

            var buffer = new byte[6];

            BitFieldSerializer.Serialize(bf, buffer, 0);

            Assert.Equal(6, buffer[0]);
            Assert.Equal(0, buffer[1]);
            Assert.Equal(1, buffer[2]);
            Assert.Equal(1, buffer[3]);
            Assert.Equal(1, buffer[4]);
            Assert.Equal(1, buffer[5]);
        }

        [Fact]
        public void Deserializes_From_Buffer_Correctly()
        {
            var buffer = new byte[]
            {
                6, // Size of the serialized bit field in bytes.
                0,
                1, // Bit field bytes. 
                2,
                3,
                4
            };

            var bf = BitFieldSerializer.Deserialize(buffer, 0);

            Assert.Equal(4, bf.BytesLength);
            Assert.Equal(1, bf.GetByteAtIndex(0));
            Assert.Equal(2, bf.GetByteAtIndex(1));
            Assert.Equal(3, bf.GetByteAtIndex(2));
            Assert.Equal(4, bf.GetByteAtIndex(3));
        }

        [Fact]
        public void Get_Size_From_Value_Returns_Bit_Field_Size_In_And_Size_Field_Bytes()
        {
            Assert.Equal(18, BitFieldSerializer.GetSizeFromValue(new BitField(16)));
        }

        [Fact]
        public void Get_Size_From_Buffer_Returns_Bit_Field_Size_And_Size_Field_In_Bytes()
        {
            var buffer = new byte[]
            {
                6, // Size of the bit field in bytes.
                0,
                1, // Bit field bytes. 
                2,
                3,
                4
            };

            Assert.Equal(6, BitFieldSerializer.GetSizeFromBuffer(buffer, 0));
        }
    }
}