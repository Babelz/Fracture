using System;
using System.Collections;
using System.Collections.Generic;

namespace Fracture.Common.Collections.Concurrent
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
        {
            get;
            private set;
        }
        #endregion
        
        public LinearGrowthList(int bucketSize = 16, int initialBuckets = 1)
            => items = new LinearGrowthArray<T>(bucketSize, initialBuckets);

        public void Add(in T value)
        {
            if (Count >= items.Length)
                items.Grow();
            
            items.Insert(Count++, value);
        }
        
        public bool Remove(in T value)
        {
            var index = items.IndexOf(value);
            
            if (index < 0)
                return false;
            
            items.Insert(index, default);
            
            return true;
        }
        
        public ref T AtIndex(int index)
            => ref items.AtIndex(index);
        
        public void Insert(int index, in T value)
            => items.Insert(index, value);
        
        public IEnumerator<T> GetEnumerator()
            => items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => items.GetEnumerator();
    }
}