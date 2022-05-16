using System;
using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Xunit;

namespace Fracture.Common.Tests.Memory
{
    [Trait("Category", "Memory")]
    public class CollectionPoolTests
    {
        #region Invalid test type
        /// <summary>
        /// Invalid test type. Invalid because it does not implement generic collection interface.
        /// </summary>
        public sealed class InvalidTestClass
        {
            public InvalidTestClass()
            {
            }
        }
        #endregion

        #region Fields
        private readonly CollectionPool<List<int>> pool;
        #endregion

        public CollectionPoolTests()
        {
            pool = new CollectionPool<List<int>>(new Pool<List<int>>(new LinearStorageObject<List<int>>(new LinearGrowthArray<List<int>>())));
        }

        [Fact]
        public void Ctor_Throws_If_Element_Does_Not_Implement_Generic_Collection_Interface()
            => Assert.Throws<ArgumentException>(() => new CollectionPool<InvalidTestClass>(
                                                    new Pool<InvalidTestClass>(
                                                        new LinearStorageObject<InvalidTestClass>(new LinearGrowthArray<InvalidTestClass>())))
            );

        [Fact]
        public void Take_Always_Returns_Empty_Collection()
        {
            Assert.Empty(pool.Take());
            Assert.Empty(pool.Take());
            Assert.Empty(pool.Take());
        }

        [Fact]
        public void Return_Always_Clears_Collection()
        {
            var list = pool.Take();

            list.AddRange(new [] { 20, 30, 40 });

            pool.Return(list);

            Assert.Empty(list);
        }
    }
}