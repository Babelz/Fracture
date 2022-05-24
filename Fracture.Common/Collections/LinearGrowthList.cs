using System.Collections;
using System.Collections.Generic;

namespace Fracture.Common.Collections
{
    /// <summary>
    /// List that grows in linear manner by using <see cref="LinearGrowthArray{T}"/> for it's internal storage.
    /// </summary>
    public sealed class LinearGrowthList<T> : IEnumerable<T>
    {
        #region Fields
        private readonly LinearGrowthArray<T> items;
        #endregion

        #region Properties
        public int Count
            => items.Length;
        #endregion

        public LinearGrowthList(int bucketSize = 16, int initialBuckets = 1)
            => items = new LinearGrowthArray<T>(bucketSize, initialBuckets);
        
        public ref T AtIndex(int index)
        {
            while (index >= items.Length)
                items.Grow();

            return ref items.AtIndex (index);
        }
        
        /// <summary>
        /// Inserts given item to given index. This insert function allows inserting past the collection. When inserting past
        /// the collection bounds the collection will grow to fit to the index that is out of the current bounds.
        /// </summary>
        public void Insert(int index, in T value)
        {
            while (index >= items.Length)
                items.Grow();

            items.Insert(index, value);
        }

        public IEnumerator<T> GetEnumerator()
            => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => items.GetEnumerator();
    }
}