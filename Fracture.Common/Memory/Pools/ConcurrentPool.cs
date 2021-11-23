using System;
using System.Collections.Generic;

namespace Fracture.Common.Memory.Pools
{
    public sealed class ConcurrentPool<T> : IPool<T> where T : class
    {
        #region Fields
        private readonly IPool<T> pool;
        #endregion

        public ConcurrentPool(IPool<T> pool)
            => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        
        public void Return(T element)
        {
            lock (pool) pool.Return(element);
        }

        public T Take(PoolElementDecoratorDelegate<T> decorator = null)
        {
            lock (pool) return pool.Take(decorator);
        }
    }
}
