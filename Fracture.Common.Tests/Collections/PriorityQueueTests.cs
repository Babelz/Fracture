using System;
using System.Linq;
using Fracture.Common.Collections;
using Xunit;

namespace Fracture.Common.Tests.Collections
{
    [Trait("Category", "Collections")]
    public class PriorityQueueTests
    {
        public PriorityQueueTests()
        {
        }
        
        [Fact]
        public void Ctor_Throws_If_Capacity_Is_Negative()
            => Assert.NotNull(Record.Exception(() => new PriorityQueue<int>((a, b) => a - b, -200)));
        
        [Fact]
        public void Ctor_Throws_If_Capacity_Is_Zero()
            => Assert.NotNull(Record.Exception(() => new PriorityQueue<int>((a, b) => a - b, 0)));
        
        [Fact]
        public void Ctor_Throws_If_Comparer_Is_Null()
            => Assert.NotNull(Record.Exception(() => new PriorityQueue<int>(null)));
        
        [Fact]
        public void Items_Are_Stored_In_Order()
        {
            var queue = new PriorityQueue<int>((a, b) => a - b)
            {
                50,
                30,
                20,
                80,
                90,
                100,
                40,
                60,
                10,
                70
            };
            
            Assert.Equal(new [] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, queue.ToArray());
        }
        
        [Fact]
        public void Count_Should_Not_Be_Zero_Based_Index_Value()
        {
            var queue = new PriorityQueue<int>((a, b) => a - b)
            {
                50,
                30,
                20,
                80,
                90,
                100
            };
            
            Assert.Equal(6, queue.Count);
        }
        
        [Fact]
        public void Remove_Should_Not_Leave_Holes()
        {
            var queue = new PriorityQueue<int>((a, b) => a - b)
            {
                1, 2, 5, 8, 3, 10, 9, 7, 6, 4
            };
            
            // Remove from end.
            Assert.True(queue.Remove(9));
            Assert.Equal(new [] { 1, 2, 3, 4, 5, 6, 7, 8, 10 }, queue.ToArray());
            
            // Remove from middle.
            Assert.True(queue.Remove(4));
            Assert.Equal(new [] { 1, 2, 3, 5, 6, 7, 8, 10 }, queue.ToArray());
            
            // Remove from beginning.
            Assert.True(queue.Remove(1));
            Assert.Equal(new [] { 2, 3, 5, 6, 7, 8, 10 }, queue.ToArray());
        }
        
        [Fact]
        public void Remove_Should_Keep_Head_And_Tail_In_Sync()
        {
            var queue = new PriorityQueue<int>((a, b) => a - b)
            {
                1, 2, 5, 8, 3, 10, 9, 7, 6, 4
            };
            
            // Remove from end.
            Assert.True(queue.Remove(9));
            Assert.Equal(new [] { 1, 2, 3, 4, 5, 6, 7, 8, 10 }, queue.ToArray());
            
            queue.AddRange(20, 25, 30);
            
            // Remove from middle.
            Assert.True(queue.Remove(4));
            Assert.Equal(new [] { 1, 2, 3, 5, 6, 7, 8, 10, 20, 25, 30 }, queue.ToArray());
            
            queue.AddRange(11, 15, 18);
            
            // Remove from beginning.
            Assert.True(queue.Remove(1));
            Assert.Equal(new [] { 2, 3, 5, 6, 7, 8, 10, 11, 15, 18, 20, 25, 30 }, queue.ToArray());
        }
    }
}