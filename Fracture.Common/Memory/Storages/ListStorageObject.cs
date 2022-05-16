using System;
using System.Collections.Generic;

namespace Fracture.Common.Memory.Storages
{
    /// <summary>
    /// Storage object that uses <see cref="List{T}"/> as its internal storage.
    /// </summary>
    public sealed class ListStorageObject<T> : IStorageObject<T> where T : class
    {
        #region Fields
        private readonly IList<T> storage;
        #endregion

        #region Properties
        public bool Empty => storage.Count == 0;

        public int Count => storage.Count;
        #endregion

        public ListStorageObject(IList<T> storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public void Return(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            storage.Add(element);
        }

        public T Take()
        {
            if (Empty)
                throw new InvalidOperationException("storage is empty");

            var element = storage[0];

            storage.RemoveAt(0);

            return element;
        }
    }
}