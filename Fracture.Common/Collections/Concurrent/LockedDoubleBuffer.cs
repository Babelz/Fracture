using System;
using System.Collections.Generic;
using Fracture.Common.Events;

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

        private int head;

        private T[] writeBuffer;
        private T[] readBuffer;
        #endregion

        public LockedDoubleBuffer(int capacity = 8)
        {
            swapLock = new object();

            writeBuffer = new T[capacity];
            readBuffer  = new T[capacity];
        }

        /// <summary>
        /// Pushes value to the buffer. To read the contents call <see cref="Read"/>.
        /// </summary>
        public void Push(T value)
        {
            lock (swapLock)
            {
                if (head >= writeBuffer.Length)
                    Array.Resize(ref writeBuffer, writeBuffer.Length * 2);

                writeBuffer[head++] = value;
            }
        }

        /// <summary>
        /// Returns the contents of the buffer to the caller. Clears and swaps internal state.
        /// </summary>
        public Span<T> Read()
        {
            lock (swapLock)
            {
                if (head >= readBuffer.Length)
                    Array.Resize(ref readBuffer, head);

                // Swap write and read buffers for second thread to continue on writing and 
                // second thread to continue on writing.
                (writeBuffer, readBuffer) = (readBuffer, writeBuffer);

                // Reset head for rewriting and return results.
                var result = new Span<T>(readBuffer, 0, head);

                head = 0;

                return result;
            }
        }
    }
}