using System;
using System.Collections.Generic;

namespace Fracture.Common.Collections
{
    /// <summary>
    /// Data structure that dynamically allocates new items as required
    /// and can be used to store existing items for later use.
    /// </summary>
    public sealed class FreeList<T>
    {
        #region Fields
        private readonly Stack<T> free;

        private readonly Func<T> next;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="FreeList{T}"/> using given generator function.
        /// </summary>
        public FreeList(Func<T> next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));

            free = new Stack<T>();
        }

        /// <summary>
        /// Takes next item from the free list.
        /// </summary>
        public T Take()
            => free.Count != 0 ? free.Pop() : next();

        /// <summary>
        /// Returns given item to the free list.
        /// </summary>
        public void Return(T element)
            => free.Push(element);
    }
}