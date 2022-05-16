using System;
using Fracture.Common.Memory.Storages;
using Fracture.Common.Reflection;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>    
    /// Pool that attempts to keep elements in some what linear memory alignment.
    /// Elements are not guaranteed to be linearly allocated, but the pool attempts the elements to be allocated
    /// in some what linear manner by pre-allocating more than one object when storage is empty.
    /// </summary>
    public sealed class LinearPool<T> : IPool<T> where T : class
    {
        #region Fields
        private readonly Func<T> instantiate;

        private readonly IStorageObject<T> storage;

        private readonly int allocations;
        #endregion

        /// <summary>
        /// Creates new linear pool using given delegate for allocations.
        /// </summary>
        public LinearPool(IStorageObject<T> storage, Func<T> instantiate, int allocations)
        {
            if (allocations <= 0)
                throw new ArgumentOutOfRangeException(nameof(allocations));

            this.storage     = storage ?? throw new ArgumentNullException(nameof(storage));
            this.instantiate = instantiate ?? throw new ArgumentNullException(nameof(instantiate));
            this.allocations = allocations;

            Allocate();
        }

        /// <summary>
        /// Creates new linear pool allocating new objects with public parameterless constructor. In case
        /// no constructor exists, an exception will be thrown.
        /// </summary>
        public LinearPool(IStorageObject<T> storage, int allocations)
            : this(storage,
                   (Func<T>)DynamicConstructorBinder.Bind(typeof(T).GetConstructor(Type.EmptyTypes), typeof(Func<T>)),
                   allocations)
        {
        }

        private void Allocate()
        {
            if (storage.Count != 0)
                return;

            var i = 0;

            while (i < allocations)
            {
                storage.Return(instantiate());

                i++;
            }
        }

        public void Return(T element) => storage.Return(element);

        public T Take(PoolElementDecoratorDelegate<T> decorator = null)
        {
            if (storage.Count == 0)
                Allocate();

            var element = storage.Take();

            decorator?.Invoke(element);

            return element;
        }
    }
}