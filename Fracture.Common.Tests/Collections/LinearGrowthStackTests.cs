using System;
using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    public class LinearGrowthStackTests
    {
        #region Fields
        private readonly LinearGrowthStack<int> stack;
        #endregion

        public LinearGrowthStackTests()
            => stack = new LinearGrowthStack<int>(new LinearGrowthArray<int>(2));

        [Fact]
        public void LinearGrowthStack_Ctor_Test()
        {
            Assert.Throws<ArgumentNullException>(() => new LinearGrowthStack<int>(null));

            try
            {
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new LinearGrowthStack<int>();

                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new LinearGrowthStack<int>(new LinearGrowthArray<int>(8));
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void Peek_Test()
        {
            stack.Push(0);
            stack.Push(1);
            stack.Push(2);

            Assert.Equal(2, stack.Peek());

            stack.Pop();

            Assert.Equal(1, stack.Peek());

            stack.Pop();

            Assert.Equal(0, stack.Peek());

            stack.Pop();
        }

        [Fact]
        public void Pop_Test()
        {
            stack.Push(0);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            stack.Push(4);

            Assert.Equal(5, stack.Top);
            Assert.False(stack.Empty);

            Assert.Equal(4, stack.Pop());
            Assert.Equal(3, stack.Pop());
            Assert.Equal(2, stack.Pop());
            Assert.Equal(1, stack.Pop());
            Assert.Equal(0, stack.Pop());

            Assert.Equal(0, stack.Top);
            Assert.True(stack.Empty);
        }

        [Fact]
        public void Push_Test()
        {
            Assert.Equal(0, stack.Top);
            Assert.True(stack.Empty);

            stack.Push(0);

            Assert.Equal(1, stack.Top);
            Assert.False(stack.Empty);

            stack.Push(1);

            Assert.Equal(2, stack.Top);
            Assert.False(stack.Empty);

            stack.Push(2);

            Assert.Equal(3, stack.Top);
            Assert.False(stack.Empty);

            stack.Push(3);

            Assert.Equal(4, stack.Top);
            Assert.False(stack.Empty);
        }
    }
}