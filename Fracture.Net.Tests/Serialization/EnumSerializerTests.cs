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
            FooBar,
        }

        private enum EnumWithExplicitUIntType : uint
        {
            Foo = uint.MinValue,
            Bar,
            Baz,
            FooBar = uint.MaxValue,
        }

        private enum EnumWithImplicitIntType
        {
            Foo    = 0,
            Bar    = 1,
            Baz    = 2,
            FooBar = 3,
        }
        #endregion

        public EnumSerializerTests()
        {
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

            Assert.Equal(0, MemoryMapper.ReadInt(buffer, 0));
            Assert.Equal(1, MemoryMapper.ReadInt(buffer, sizeof(int)));
            Assert.Equal(2, MemoryMapper.ReadInt(buffer, sizeof(int) * 2));
            Assert.Equal(3, MemoryMapper.ReadInt(buffer, sizeof(int) * 3));
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

            Assert.Equal(uint.MinValue, MemoryMapper.ReadUint(buffer, 0));
            Assert.Equal(1u, MemoryMapper.ReadUint(buffer, sizeof(uint)));
            Assert.Equal(2u, MemoryMapper.ReadUint(buffer, sizeof(uint) * 2));
            Assert.Equal(uint.MaxValue, MemoryMapper.ReadUint(buffer, sizeof(uint) * 3));
        }
    }
}