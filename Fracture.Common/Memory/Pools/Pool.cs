using System;
using Fracture.Common.Memory.Storages;
using Fracture.Common.Reflection;

namespace Fracture.Common.Memory.Pools
{
    public delegate void PoolElementDecoratorDelegate<in T>(T element);

    /// <summary>
    /// Generic interface for implementing generic objects pools.
    /// </summary>
    public interface IPool<T> where T : class
    {
        /// <summary>
        /// Takes next available element from the pool.
        /// </summary>
        T Take(PoolElementDecoratorDelegate<T> decorator = null);
        
        /// <summary>
        /// Returns given element to the pool.
        /// </summary>
        void Return(T element);
    }

    /// <summary>
    /// Base class for implementing generic object pools.
    /// </summary>
    public abstract class PoolBase<T> : IPool<T> where T : class
    {
        #region Fields
        private readonly int capacity;

        /// <summary>
        /// Storage for holding free objects.
        /// </summary>
        private readonly IStorageObject<T> storage;
        #endregion

        protected PoolBase(IStorageObject<T> storage, int initialStoredObjectsCount, int capacity)
        {
            if (initialStoredObjectsCount < 0) throw new ArgumentOutOfRangeException(nameof(initialStoredObjectsCount));
            if (capacity < 0)                  throw new ArgumentOutOfRangeException(nameof(capacity));

            this.storage  = storage ?? throw new ArgumentNullException(nameof(PoolBase<T>.storage));
            this.capacity = capacity;
        }
        
        protected void CreateObjects(int count)
        {
            if (count == 0) return;
            
            for (var i = 0; i < count; i++) storage.Return(New());
        }

        protected abstract T New();

        /// <summary>
        /// Takes next object from the pool.
        /// </summary>
        public T Take(PoolElementDecoratorDelegate<T> decorator = null)
        {
            var element = storage.Empty ? New() : storage.Take();
            
            decorator?.Invoke(element);
            
            return element;
        }

        /// <summary>
        /// Returns given object to pool.
        /// </summary>
        public void Return(T element)
        {
            if (capacity != 0 && storage.Count + 1 >= capacity)
                return;
            
            storage.Return(element);
        }
    }
        
    /// <summary>
    /// Basic generic pool that uses dynamic constructor binding for creating new objects.
    /// </summary>
    public sealed class Pool<T> : PoolBase<T> where T : class, new()
    {
        #region Fields
        private readonly Func<T> instantiate;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="Pool{T}"/> with given initial objects count and
        /// optional capacity.
        /// </summary>
        public Pool(IStorageObject<T> storage, int initialStoredObjectsCount, int capacity)
            : base(storage, initialStoredObjectsCount, capacity)
        {
            // Wrap constructor call to delegate, this should be faster than calling new on the generic T.
            instantiate = (Func<T>)DynamicConstructorBinder.Bind(typeof(T).GetConstructor(Type.EmptyTypes), typeof(Func<T>));

            CreateObjects(initialStoredObjectsCount);
        }

        /// <summary>
        /// Creates new instance of <see cref="Pool{T}"/> with given initial objects and
        /// zero capacity.
        /// </summary>
        public Pool(IStorageObject<T> storage, int initialStoredObjectsCount)
            : this(storage, initialStoredObjectsCount, 0)
        {
        }

        /// <summary>
        /// Creates new instance of <see cref="Pool{T}"/> with zero capacity and initial objects.
        /// </summary>
        public Pool(IStorageObject<T> storage)
            : this(storage, 0, 0)
        {
        }

        protected override T New() => instantiate();
    }
}
