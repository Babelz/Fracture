using System.Collections.Generic;

namespace Fracture.Common.Collections.Concurrent
{
    /// <summary>
    /// Buffer that allows one thread to write and one thread to read from it in thread safe manner.
    /// </summary>
    public sealed class LockedDoubleBuffer<T>
    {
        #region Fields
        // Lock object used to lock the buffer.
        private readonly object swapLock;

        private List<T> a;
        private List<T> b;
        #endregion

        public LockedDoubleBuffer()
        {
            swapLock = new object();

            a = new List<T>();
            b = new List<T>();
        }

        /// <summary>
        /// Pushes value to the buffer. To read the contents call <see cref="Read"/>.
        /// </summary>
        public void Push(T value)
        {
            lock (swapLock)
                a.Add(value);
        }

        /// <summary>
        /// Returns the contents of the buffer to the caller. Clears and swaps internal state.
        /// </summary>
        public T[] Read()
        {
            lock (swapLock)
            {
                var temp   = a;
                var result = temp.ToArray();

                temp.Clear();

                a = b;
                b = temp;

                return result;
            }
        }
    }
}