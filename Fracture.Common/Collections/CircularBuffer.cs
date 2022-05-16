using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Collections
{
    /// <summary>
    /// Data structured that is fixed size buffer that uses its storage in circular manner.
    /// </summary>
    public sealed class CircularBuffer<T> : IEnumerable<T>
    {
        #region Fields
        private readonly T [] items;

        // The current head index, head index
        // always points to the next index where writing occurs.
        private int headIndex;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the value last written.
        /// </summary>
        public T Head => AtOffset(0);

        public int Capacity
        {
            get;
        }
        #endregion

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "expecting positive non-zero value");

            Capacity = capacity;
            items    = new T[capacity];
        }

        /// <summary>
        /// Rotates the index inside the given length and with given offset. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Rotate(int head, int length, int offset)
        {
            // Compute actual index.
            var location = head + offset;

            // Get the index using modulo.
            location = (location % length + length) % length;

            return location;
        }

        public ref T AtOffset(int offset) => ref items[Rotate(headIndex - 1, items.Length, offset)];

        public void Push(T item)
        {
            if (headIndex >= items.Length)
                headIndex = 0;

            items[headIndex++] = item;
        }

        public void Clear()
        {
            headIndex = 0;

            Array.Clear(items, 0, items.Length);
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}