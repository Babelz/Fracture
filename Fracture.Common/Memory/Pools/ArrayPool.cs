using System;
using System.Collections.Generic;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Interface for creating classes fo pooling arrays that contain elements of type <see cref="T"/>. Pool
    /// will always ensure that it returns array that is specifically of requested size.
    /// </summary>
    public interface IArrayPool<T>
    {
        void Return(T[] array);

        T[] Take(int size);
    }
    
    /// <summary>
    /// Default implementation of <see cref="IArrayPool{T}"/>.
    /// </summary>
    public sealed class ArrayPool<T> : IArrayPool<T>
    {
        #region Fields
        private readonly int storageCapacity;

        private readonly Func<IStorageObject<T[]>> factory;

        private readonly List<IStorageObject<T[]>> storages;
        #endregion

        /// <summary>
        /// Creates new instance of array pool with initial max storage size
        /// and storage capacity.
        /// </summary>
        public ArrayPool(Func<IStorageObject<T[]>> factory, int initialMaxStorageSize, int storageCapacity)
        {
            if (initialMaxStorageSize < 0) throw new ArgumentOutOfRangeException(nameof(initialMaxStorageSize));
            if (storageCapacity < 0)       throw new ArgumentOutOfRangeException(nameof(storageCapacity));

            this.factory         = factory ?? throw new ArgumentNullException(nameof(factory));
            this.storageCapacity = storageCapacity;
            
            storages = new List<IStorageObject<T[]>>();

            for (var i = 0; i < initialMaxStorageSize; i++) storages.Add(factory());
        }
        /// <summary>
        /// Creates new instance of array pool with storage capacity.
        /// </summary>
        public ArrayPool(Func<IStorageObject<T[]>> factory, int initialMaxStorageSize)
            : this(factory, initialMaxStorageSize, 0)
        {
        }

        /// <summary>
        /// Returns pre-created array or creates new
        /// if there are any arrays free.
        /// </summary>
        private static T[] GetNextFreeArray(IStorageObject<T[]> storage, int size)
        {
            if (storage.Empty) return new T[size];

            // Get the array.
            var array = storage.Take();

            Array.Clear(array, 0, array.Length);
            
            return array;
        }

        /// <summary>
        /// Grows the storage size if required
        /// and returns the array storage object.
        /// </summary>
        private IStorageObject<T[]> GetArrayStorage(int size)
        {
            // Grow storage size.
            while (size >= storages.Count) storages.Add(factory());
            
            return storages[size];
        }
        
        public T[] Take(int size)
        {
            // Reduce 1 to get 0 index working.
            size = Math.Max(0, size - 1);

            var storage = GetArrayStorage(size);

            // Add one to keep the size correct.
            return GetNextFreeArray(storage, size + 1);
        }

        public void Return(T[] array)
        {
            var storage = GetArrayStorage(array.Length - 1);

            if (storageCapacity != 0 && storage.Count >= storageCapacity)
                return;
            
            storage.Return(array);
        }
    }
    
    /// <summary>
    /// Array pool that uses locking for thread safe operations.
    /// </summary>
    public sealed class ConcurrentArrayPool<T> : IArrayPool<T>
    {
        #region Fields
        private readonly IArrayPool<T> pool;
        #endregion

        public ConcurrentArrayPool(IArrayPool<T> pool)
            => this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        
        public void Return(T[] array)
        {
            lock (pool) pool.Return(array);
        }

        public T[] Take(int size)
        {
            lock (pool) return pool.Take(size);
        }
    }
}
