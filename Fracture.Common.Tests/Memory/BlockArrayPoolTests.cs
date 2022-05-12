using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Xunit;

namespace Fracture.Common.Tests.Memory
{
    [Trait("Category", "Memory")]
    public class BlockArrayPoolTests
    {
        #region Fields
        private readonly BlockArrayPool<int> pool;
        #endregion
        
        public BlockArrayPoolTests()
        {
            pool = new BlockArrayPool<int>(
                new ArrayPool<int>(
                    () => new LinearStorageObject<int[]>(new LinearGrowthArray<int[]>(8, 1)), 1), 
                32, 1024);
        }
        
        [Fact]
        public void Take_Returns_Array_Of_Supplied_Size_If_It_Exceeds_Max_Size()
        {
            Assert.Equal(2000, pool.Take(2000).Length);
            Assert.Equal(1025, pool.Take(1025).Length);
        }
        
        [Fact]
        public void Take_Returns_Block_Sized_Array_If_Supplied_Size_Is_Smaller()
        {
            Assert.Equal(64, pool.Take(52).Length);
            Assert.Equal(1024, pool.Take(1000).Length);
            Assert.Equal(512, pool.Take(491).Length);
        }
        
        [Fact]
        public void Return_Disposes_Arrays_Larger_Than_Max_Size()
        {
            var array = pool.Take(3400);
            
            pool.Return(array);
            
            Assert.NotSame(array, pool.Take(3400));
        }
        
        [Fact]
        public void Return_Stores_Arrays_Smaller_Than_Max_Size()
        {
            var array = pool.Take(52);
            
            pool.Return(array);
            
            Assert.Same(array, pool.Take(52));
        }
    }
}