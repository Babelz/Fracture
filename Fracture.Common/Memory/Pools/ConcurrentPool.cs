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

        public void Return(IList<T> elements)
        {
            lock (pool) pool.Return(elements);
        }

        public void Return(IList<T> elements, int start, int count)
        {
            lock (pool) pool.Return(elements, start, count);
        }

        public T Take()
        {
            lock (pool) return pool.Take();
        }

        public void Take(IList<T> elements)
        {
            lock (pool) pool.Take(elements);
        }
    }
}
