using System;
using Fracture.Common.Memory;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public class EnumSerializerTests
    {
        #region Test types
        private enum EnumWithExplicitByteType : byte
        {
            Foo,
            Bar,
            Baz,
            FooBar
        }
        
        private enum EnumWithExplicitUIntType : uint
        {
            Foo = uint.MinValue,
            Bar,
            Baz,
            FooBar = uint.MaxValue
        }
        
        private enum EnumWithImplicitIntType
        {
            Foo    = 0,
            Bar    = 1,
            Baz    = 2,
            FooBar = 3
        }
        #endregion

        public EnumSerializerTests()
        {
        }

        [Fact]
        public void Serialized_Enum_Contains_Small_Content_Length()
        {
            var buffer = new byte[32];
            
            EnumSerializer.Serialize(EnumWithExplicitUIntType.FooBar, buffer, 0);
            
            EnumSerializer.Serialize(EnumWithExplicitByteType.FooBar, buffer, EnumSerializer.GetSizeFromValue(EnumWithExplicitUIntType.FooBar));

            Assert.Equal(4, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(1, MemoryMapper.ReadByte(buffer, EnumSerializer.GetSizeFromValue(EnumWithExplicitUIntType.FooBar)));
        }
        
        [Fact]
        public void Serializes_Implicit_Enums_Correctly()
        {
            var buffer = new byte[32];
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.Foo, buffer, 0);
            var offset = EnumSerializer.GetSizeFromValue(EnumWithImplicitIntType.Foo);
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.Bar, buffer, offset);
            offset += EnumSerializer.GetSizeFromValue(EnumWithImplicitIntType.Bar);
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.Baz, buffer, offset);
            offset += EnumSerializer.GetSizeFromValue(EnumWithImplicitIntType.Baz);
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.FooBar, buffer, offset);

            Assert.Equal(4, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(0, MemoryMapper.ReadInt(buffer, sizeof(byte)));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) + sizeof(int)));
            Assert.Equal(1, MemoryMapper.ReadInt(buffer, sizeof(byte) * 2 + sizeof(int)));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 2 + sizeof(int) * 2));
            Assert.Equal(2, MemoryMapper.ReadInt(buffer, sizeof(byte) * 3 + sizeof(int) * 2));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 3 + sizeof(int) * 3));
            Assert.Equal(3, MemoryMapper.ReadInt(buffer, sizeof(byte) * 4 + sizeof(int) * 3));
        }

        [Fact]
        public void Serializes_Explicit_Enums_Correctly()
        {
            var buffer = new byte[32];
            
            EnumSerializer.Serialize(EnumWithExplicitUIntType.Foo, buffer, 0);
            var offset = EnumSerializer.GetSizeFromValue(EnumWithExplicitUIntType.Foo);
            
            EnumSerializer.Serialize(EnumWithExplicitUIntType.Bar, buffer, offset);
            offset += EnumSerializer.GetSizeFromValue(EnumWithExplicitUIntType.Bar);
            
            EnumSerializer.Serialize(EnumWithExplicitUIntType.Baz, buffer, offset);
            offset += EnumSerializer.GetSizeFromValue(EnumWithExplicitUIntType.Baz);
            
            EnumSerializer.Serialize(EnumWithExplicitUIntType.FooBar, buffer, offset);

            Assert.Equal(4, MemoryMapper.ReadByte(buffer, 0));
            Assert.Equal(uint.MinValue, MemoryMapper.ReadUint(buffer, sizeof(byte)));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) + sizeof(uint)));
            Assert.Equal(1u, MemoryMapper.ReadUint(buffer, sizeof(byte) * 2 + sizeof(uint)));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 2 + sizeof(uint) * 2));
            Assert.Equal(2u, MemoryMapper.ReadUint(buffer, sizeof(byte) * 3 + sizeof(uint) * 2));
            
            Assert.Equal(4, MemoryMapper.ReadByte(buffer, sizeof(byte) * 3 + sizeof(uint) * 3));
            Assert.Equal(uint.MaxValue, MemoryMapper.ReadUint(buffer, sizeof(byte) * 4 + sizeof(uint) * 3));
        }
    }
}