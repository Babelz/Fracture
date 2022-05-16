using System;
using System.Collections.Generic;
using Fracture.Common.Collections;

namespace Fracture.Common.Memory.Storages
{
    /// <summary>
    /// Storage object that uses <see cref="Stack{T}"/> as its internal storage object.
    /// </summary>
    public sealed class StackStorageObject<T> : IStorageObject<T> where T : class
    {
        #region Fields
        private readonly IStack<T> storage;
        #endregion

        #region Properties
        public bool Empty => storage.Empty;

        public int Count => storage.Top;
        #endregion

        public StackStorageObject(IStack<T> storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        public void Return(T element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            storage.Push(element);
        }

        public T Take() => !Empty ? storage.Pop() : throw new InvalidOperationException("storage is empty");
    }
}