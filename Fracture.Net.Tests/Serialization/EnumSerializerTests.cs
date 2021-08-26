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
            FooBar = 4
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
            
            offset = EnumSerializer.GetSizeFromValue(EnumWithImplicitIntType.Foo);
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.Baz, buffer, offset);
            
            offset = EnumSerializer.GetSizeFromValue(EnumWithImplicitIntType.Foo);
            
            EnumSerializer.Serialize(EnumWithImplicitIntType.FooBar, buffer, offset);
        }

        [Fact]
        public void Serializes_Explicit_Enums_Correctly()
        {
        }
    }
}