using System;
using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    public class LinearGrowthArrayTests
    {
        #region Fields
        private readonly LinearGrowthArray<int> array;
        #endregion

        public LinearGrowthArrayTests()
            => array = new LinearGrowthArray<int>(4);

        [Fact]
        public void LinearGrowthArray_Ctor_Test()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearGrowthArray<int>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LinearGrowthArray<int>(1, -1));

            try
            {
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new LinearGrowthArray<int>(8);

                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new LinearGrowthArray<int>(8);
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void Grow_Creates_New_Buckets()
        {
            Assert.Equal(1, array.Buckets);

            array.Grow(3);

            Assert.Equal(4, array.Buckets);
        }

        [Fact]
        public void SetElementAtIndex_Throws_Correctly_If_Index_Is_Out_Of_Range()
        {
            array.Insert(0, 1);

            Assert.Throws<IndexOutOfRangeException>(() => array.Insert(12, 1));
            Assert.Throws<IndexOutOfRangeException>(() => array.Insert(13, 1));

            array.Grow(10);

            try
            {
                array.Insert(12, 1);

                array.Insert(13, 1);
            }
            catch
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void GetElementAtIndex_Throws_If_Index_Is_Out_Of_Range()
        {
            array.Insert(0, 1);
            array.Insert(1, 2);
            array.Insert(2, 3);
            array.Insert(3, 4);

            Assert.Equal(1, array.AtIndex(0));
            Assert.Equal(2, array.AtIndex(1));
            Assert.Equal(3, array.AtIndex(2));
            Assert.Equal(4, array.AtIndex(3));

            Assert.Equal(1, array.Buckets);

            Assert.Throws<IndexOutOfRangeException>(() => array.Insert(6, 1));
            Assert.Throws<IndexOutOfRangeException>(() => array.Insert(7, 1));

            Assert.Throws<IndexOutOfRangeException>(() => array.AtIndex(6));
            Assert.Throws<IndexOutOfRangeException>(() => array.AtIndex(7));

            array.Grow(4);

            Assert.Equal(1, array.AtIndex(0));
            Assert.Equal(2, array.AtIndex(1));
            Assert.Equal(3, array.AtIndex(2));
            Assert.Equal(4, array.AtIndex(3));

            try
            {
                array.Insert(6, 1);
                array.Insert(7, 1);

                Assert.Equal(1, array.AtIndex(6));
                Assert.Equal(1, array.AtIndex(7));
            }
            catch
            {
                Assert.True(false);
            }
        }
    }
}