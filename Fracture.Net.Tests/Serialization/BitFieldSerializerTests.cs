using System.ComponentModel;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public class BitFieldTests
    {
        [Fact]
        public void Writes_Bits_To_Correct_Bytes()
        {
            var bf = new BitField(16);
            
            bf.SetBit(8 * 8, false);
            
            Assert.Equal(128, bf.GetByteAtIndex(8));
        }
        
        [Fact]
        public void Reads_Bits_From_Correct_Bytes()
        {
        }
        
        [Fact]
        public void Reads_Bits_Past_64_Bits()
        {
        }
        
        [Fact]
        public void Writes_Bits_Past_64_Bits()
        {
        }
        
        [Fact]
        public void Writes_Bits_From_Left_To_Right()
        {
        }
        
        [Fact]
        public void Reads_Bits_From_Left_To_Right()
        {
        }
        
        [Fact]
        public void Copies_Bytes_To_Buffer_In_Order()
        {
        }
        
        [Fact]
        public void Copies_Bytes_From_Buffer_In_Order()
        {
        }
    }
    
    public class BitFieldSerializerTests
    {
        
    }
}