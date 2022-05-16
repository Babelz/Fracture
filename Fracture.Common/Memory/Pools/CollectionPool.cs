using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Pool for pooling generic collections that implement the <see cref="System.Collections.ICollection"/> interface. 
    /// Guarantees that all collections take from the pool are initialized with zero elements.
    /// </summary>
    public sealed class CollectionPool<T> : IPool<T> where T : class
    {
        #region Fields
        private readonly IPool<T> pool;

        private readonly Action<T> clear;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="CollectionPool{T}"/> with.
        /// </summary>
        /// <param name="pool">inner pool that will do the actual pooling</param>
        public CollectionPool(IPool<T> pool)
        {
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));

            // Check that the element is collection or generic collection.
            if (!typeof(T).GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>)))
                throw new ArgumentException($"expecting type that implements generic {nameof(ICollection)} interface");

            // Get clear method using reflection and create clear delegate for invoking it.
            var clearMethod = typeof(T).GetMethod("Clear");

            if (clearMethod == null)
                throw new ArgumentException("expecting collection type that implements Clear method");

            clear = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), clearMethod);
        }

        public T Take(PoolElementDecoratorDelegate<T> decorator = null)
            => pool.Take(decorator);

        public void Return(T element)
        {
            pool.Return(element);

            clear(element);
        }
    }
}