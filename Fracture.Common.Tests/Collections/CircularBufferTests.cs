using System;
using System.Linq;
using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    public sealed class CircularBufferTests
    {
        public CircularBufferTests()
        {
        }

        [Fact]
        public void CircularBuffer_Ctor_Test()
        {
            Assert.IsType<ArgumentOutOfRangeException>(Record.Exception(() => new CircularBuffer<int>(0)));
            Assert.IsType<ArgumentOutOfRangeException>(Record.Exception(() => new CircularBuffer<int>(-1)));

            try
            {
                new CircularBuffer<int>(200);
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void Rotates_Head_When_Buffer_Is_Full()
        {
            var buffer = new CircularBuffer<int>(6);

            buffer.Push(10);
            buffer.Push(9);
            buffer.Push(8);

            buffer.Push(7);
            buffer.Push(6);
            buffer.Push(5);

            // This should rotate the head.
            buffer.Push(4);

            Assert.Equal(4, buffer.AtOffset(0));
            Assert.Equal(5, buffer.AtOffset(-1));
            Assert.Equal(6, buffer.AtOffset(-2));

            Assert.Equal(7, buffer.AtOffset(-3));
            Assert.Equal(8, buffer.AtOffset(-4));
            Assert.Equal(9, buffer.AtOffset(-5));
        }

        [Fact]
        public void Returns_Items_In_Correct_Order_For_Enumeration()
        {
            var buffer = new CircularBuffer<int>(6);

            buffer.Push(1);
            buffer.Push(2);
            buffer.Push(3);

            buffer.Push(4);
            buffer.Push(5);
            buffer.Push(6);

            // These will rotate and head should be at offset 2.
            buffer.Push(7);
            buffer.Push(8);
            buffer.Push(9);

            Assert.Equal(new[] { 7, 8, 9, 4, 5, 6 }, buffer.ToArray());
        }

        [Fact]
        public void Buffer_Clear_Sets_All_Values_To_Their_Defaults()
        {
            var buffer = new CircularBuffer<int>(6);

            for (var i = 1; i < 20; i++)
                buffer.Push(i);

            buffer.Clear();

            Assert.Equal(Enumerable.Repeat(0, 6).ToArray(), buffer.ToArray());
        }
    }
}