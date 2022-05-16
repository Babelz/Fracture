using System;

namespace Fracture.Common.Memory.Storages
{
    /// <summary>
    /// Storage object which storage is fixed in size.
    /// </summary>
    public sealed class FixedStorageObject<T> : IStorageObject<T> where T : class
    {
        #region Fields
        private readonly T [] storage;
        #endregion

        #region Properties
        public bool Empty => Count == 0;

        public int Count
        {
            get;
            private set;
        }
        #endregion

        public FixedStorageObject(T [] storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public void Return(T element)
        {
            if (Count >= storage.Length)
                throw new InvalidOperationException("storage is full");

            storage[Count++] = element ?? throw new ArgumentNullException(nameof(element));
        }

        public T Take()
        {
            if (Empty)
                throw new InvalidOperationException("storage is empty");

            var element = storage[--Count];

            storage[Count] = default;

            return element;
        }
    }
}