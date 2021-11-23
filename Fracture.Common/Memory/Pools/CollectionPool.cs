using System;
using System.Collections.Generic;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Pool for pooling generic collections that implement the <see cref="System.Collections.ICollection"/> interface. 
    /// Guarantees that all collections take from the pool are initialized with zero elements.
    /// </summary>
    public sealed class CollectionPool<TCollection, TValue> : IPool<TCollection> where TCollection : class, ICollection<TValue>
    {
        #region Fields
        private readonly IPool<TCollection> pool;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="CollectionPool{Collection, Value}"/> with.
        /// </summary>
        /// <param name="pool">inner pool that will do the actual pooling</param>
        public CollectionPool(IPool<TCollection> pool)
            => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        
        public TCollection Take(PoolElementDecoratorDelegate<TCollection> decorator = null) => pool.Take(decorator);
        
        public void Return(TCollection element)
        {
            pool.Return(element);

            element.Clear();
        }
    }
}
