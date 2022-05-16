using System;
using Fracture.Common.Collections;

namespace Fracture.Common.Memory.Storages
{
    /// <summary>
    /// Storage object that uses <see cref="LinearGrowthArray{T}"/> as its internal storage object.
    /// </summary>
    public sealed class LinearStorageObject<T> : IStorageObject<T> where T : class
    {
        #region Fields
        private readonly LinearGrowthArray<T> storage;
        #endregion

        #region Properties
        public bool Empty => Count == 0;

        public int Count
        {
            get;
            private set;
        }
        #endregion

        public LinearStorageObject(LinearGrowthArray<T> storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public void Return(T element)
        {
            while (Count >= storage.Length)
                storage.Grow();

            storage.Insert(Count++, element ?? throw new ArgumentNullException(nameof(element)));
        }

        public T Take()
        {
            if (Empty)
                throw new InvalidOperationException("storage is empty");

            var element = storage.AtIndex(--Count);

            storage.Insert(Count, default);

            return element;
        }
    }
}