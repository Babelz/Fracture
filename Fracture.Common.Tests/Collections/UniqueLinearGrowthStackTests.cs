using System;
using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    
    public class UniqueLinearGrowthStackTests
    {
        #region Fields
        private readonly UniqueLinearGrowthStack<int> stack;
        #endregion
        
        public UniqueLinearGrowthStackTests()
            => stack = new UniqueLinearGrowthStack<int>();

        [Fact]
        public void UniqueLinearGrowthStackTest()
        {
            Assert.Throws<ArgumentNullException>(() => new UniqueLinearGrowthStack<int>(null));

            try
            {
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new UniqueLinearGrowthStack<int>();
                
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new UniqueLinearGrowthStack<int>(new LinearGrowthStack<int>());
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void PeekTest()
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
        public void PopTest()
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
        public void PushTest()
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

        [Fact]
        public void WillThrowOnNonUniqueValue()
        {
            stack.Push(0);
            stack.Push(1);
            stack.Push(2);

            Assert.Throws<InvalidOperationException>(() => stack.Push(0));
            Assert.Throws<InvalidOperationException>(() => stack.Push(1));
            Assert.Throws<InvalidOperationException>(() => stack.Push(2));

            stack.Pop();

            try
            {
                stack.Push(2);
                stack.Push(5);
            }
            catch 
            {
                Assert.True(false);
            }
        }
    }
}