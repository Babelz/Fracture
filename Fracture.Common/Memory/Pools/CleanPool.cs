using System;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Pool that always guarantees that objects are in their initial
    /// state when they are taken from the pool.
    /// </summary>
    public sealed class CleanPool<T> : IPool<T> where T : class, IClearable
    {
        #region Fields
        private readonly IPool<T> pool;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="CleanPool{T}"/> with.
        /// </summary>
        /// <param name="pool">inner pool that will do the actual pooling</param>
        public CleanPool(IPool<T> pool) => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));

        public T Take(PoolElementDecoratorDelegate<T> decorator = null) => pool.Take(decorator);

        public void Return(T element)
        {
            pool.Return(element);

            element.Clear();
        }
    }
}