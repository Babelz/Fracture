using System;
using System.Text;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class TypeSerializerTests
    {
        public TypeSerializerTests()
        {
        }

        [Fact]
        public void Serialized_Type_Contains_Size_Of_The_Serialized_String()
        {
            var buffer = new byte[1024];

            TypeSerializer.Serialize(typeof(Console), buffer, 0);

            // 184 = typeof(Console).AssemblyQualifiedName!.Length * 2 + Protocol.ContentLength.Size.
            Assert.Equal(184, Protocol.ContentLength.Read(buffer, 0));
        }

        [Fact]
        public void Get_Size_From_Value_Returns_Type_Name_Size_In_And_Size_Field_Bytes()
            => Assert.Equal(typeof(Console).AssemblyQualifiedName!.Length * 2 + Protocol.ContentLength.Size, TypeSerializer.GetSizeFromValue(typeof(Console)));

        [Fact]
        public void Get_Size_From_Buffer_Returns_Type_Name_Size_And_Size_Field_In_Bytes()
            => Assert.Equal(10, TypeSerializer.GetSizeFromBuffer(new byte[] { 10, 0, 0, 0 }, 0));

        [Fact]
        public void Get_Size_From_Buffer_And_Get_Size_From_Value_Both_Return_Same_Value()
        {
            var buffer = new byte[1024];

            TypeSerializer.Serialize(typeof(Console), buffer, 0);

            Assert.Equal(184, TypeSerializer.GetSizeFromValue(typeof(Console)));
            Assert.Equal(184, TypeSerializer.GetSizeFromBuffer(buffer, 0));
        }
    }
}