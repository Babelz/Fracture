using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    public class FreeListTests
    {
        [Fact]
        public void FreeListTest()
        {
            try
            {
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new FreeList<int>(() => 1);
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void TakeTest()
        {
            var idc = 0;

            var list = new FreeList<int>(() => idc++);

            for (var i = 0; i < 10; i++)
                Assert.Equal(i, list.Take());
        }

        [Fact]
        public void ReturnTest()
        {
            var idc = 0;

            var list = new FreeList<int>(() => idc++);

            Assert.Equal(0, list.Take());
            Assert.Equal(1, list.Take());
            Assert.Equal(2, list.Take());
            Assert.Equal(3, list.Take());
            Assert.Equal(4, list.Take());
            Assert.Equal(5, list.Take());

            list.Return(2);
            list.Return(3);

            Assert.Equal(3, list.Take());
            Assert.Equal(2, list.Take());
            Assert.Equal(6, list.Take());
        }
    }
}