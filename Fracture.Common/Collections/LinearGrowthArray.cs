using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Collections
{
    /// <summary>
    /// Data structure that represents a simple array that works like normal arrays but grows in linear manner.
    /// Grows to the array are applied by buckets and they do not cause resizing the array as this becomes inefficient
    /// when array sizes start to get large. Contains next to nothing error handling to keep performance high.
    /// </summary>
    public sealed class LinearGrowthArray<T> : IEnumerable<T>
    {
        #region Fields
        // Scale factor for locating buckets.
        private readonly float bucketScale;

        private readonly int bucketSize;

        private T[][] buckets;
        #endregion

        #region Properties
        public int Length => buckets.Length * bucketSize;

        public int Buckets => buckets.Length;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="LinearGrowthArray{T}"/> with given bucket (page) size and 
        /// with initial count of buckets (pages).
        /// </summary>
        public LinearGrowthArray(int bucketSize = 16, int initialBuckets = 1)
        {
            // Do not allow negative or zero initial buckets.
            if (initialBuckets <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialBuckets));

            // Do not allow negative or zero bucket sizes.
            this.bucketSize = bucketSize > 0 ? bucketSize : throw new ArgumentOutOfRangeException(nameof(bucketSize));

            // Create initial buckets.
            buckets = new T[initialBuckets][];

            for (var i = 0; i < initialBuckets; i++) buckets[i] = new T[bucketSize];

            // Compute scale for computing future lookup indices to avoid division.
            bucketScale = 1.0f / bucketSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void IndexLocation(int index,
                                          float bucketScale,
                                          int bucketSize,
                                          out int bucketLocation,
                                          out int bucketIndex)
        {
            bucketLocation = (int)(index * bucketScale);
            bucketIndex    = index - bucketLocation * bucketSize;
        }

        public ref T AtIndex(int index)
        {
            IndexLocation(index, bucketScale, bucketSize, out var i, out var j);

            return ref buckets[i][j];
        }

        public void Insert(int index, in T value)
        {
            IndexLocation(index, bucketScale, bucketSize, out var i, out var j);

            buckets[i][j] = value;
        }

        public int IndexOf(in T value)
        {
            for (var i = 0; i < buckets.Length; i++)
            {
                var index = Array.BinarySearch(buckets[i], 0, bucketSize, value);

                if (index >= 0)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Grows the array from the end with given count of new buckets.
        /// </summary>
        public void Grow(int newBucketsCount = 1)
        {
            if (newBucketsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newBucketsCount));

            var oldLength = buckets.Length;

            Array.Resize(ref buckets, buckets.Length + newBucketsCount);

            for (int i = oldLength, currentLength = buckets.Length; i < currentLength; i++)
                buckets[i] = new T[bucketSize];
        }

        /// <summary>
        /// Grows the array from the beginning with given count of new buckets.
        /// </summary>
        public void Shift(int newBucketsCount = 1)
        {
#warning Test this method before using.

            if (newBucketsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(newBucketsCount));

            var shiftIndex = buckets.Length - 1;

            Array.Resize(ref buckets, buckets.Length + newBucketsCount);

            // Shift existing buckets to end.
            for (var i = buckets.Length - 1; i >= 0; i--)
                buckets[i] = buckets[shiftIndex--];

            // Create new buckets.
            for (var i = 0; i < newBucketsCount; i++)
                buckets[i] = new T[bucketSize];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];

                for (var j = 0; j < bucket.Length; j++)
                    yield return bucket[j];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}