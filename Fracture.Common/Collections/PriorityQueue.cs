using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Fracture.Common.Collections
{
    public class PriorityQueueEnumerator<T> : IEnumerator<T>
    {
        #region Fields
        private readonly T[] items;
        
        private readonly int head;
        private readonly int tail;
        
        private int index;
        #endregion
        
        #region Properties
        public T Current
            => items[index];

        object IEnumerator.Current => Current;
        #endregion

        public PriorityQueueEnumerator(T[] items, int head, int tail)
        {
            this.items = items;
            this.head  = head;
            this.tail  = tail;
            
            index = head;
        }
        
        public bool MoveNext()
            => (index + 1) <= tail;

        public void Reset()
            => index = head;
        
        public void Dispose()
        {
            // Nothing to dispose, nothing to implement.
        }
    }

    /// <summary>
    /// Class that represents a generic priority queue.
    /// </summary>
    public class PriorityQueue<T> : ICollection<T>, IEnumerable<T>
    {
        #region Fields
        private readonly Comparison<T> comparer;
        
        private T[] items;
        
        // Origin of the queue. Origin in the middle point in the array where filling begins.
        private int origin;
        
        // Head index of the queue. Always points to current head item index.
        private int head;
        
        // Tail index of the queue. Always point to next item index.
        private int tail;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the capacity of this queue. If items past the capacity are added to the queue it will resize itself.
        /// </summary>
        public int Capacity
            => items.Length;

        /// <summary>
        /// Returns count of items in the queue.
        /// </summary>
        public int Count
            => (tail - head);

        public bool IsReadOnly => false;
        #endregion

        public PriorityQueue(Comparison<T> comparer, int capacity = 16)
        {
            this.comparer = comparer ?? throw new ArgumentException(nameof(comparer));
         
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "initial capacity must be positive and greater than zero");
            
            const int MinimumCapacity = 2;
            
            if (capacity < MinimumCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(capacity)} is less than minimum capacity of {MinimumCapacity}");
            
            Resize(capacity);
        }
        
        public PriorityQueue(Comparison<T> comparer, IEnumerable<T> source, int capacity = 16)
            : this(comparer, capacity)
        {
            foreach (var item in source)
                Add(item);
        }

        private void Resize(int capacity)
        {
            if (items == null)
            {
                // Initialize empty queue.
                items = new T[capacity];
                
                origin = items.Length / 2;
                head   = origin;
                tail   = origin;
            }
            else
            {
                // Resize to new array.
                var newItems = new T[capacity];
                
                var newOrigin = newItems.Length / 2;
                
                // Shift items based on new origin.
                Array.Copy(items, head, newItems, newOrigin - (origin - head), Count);
                
                items  = newItems;
                head   = newOrigin - (origin - head);
                tail   = newOrigin + (tail - origin);
                origin = newOrigin;
            }
        } 
        
        private void SwapItems(int firstIndex, int secondIndex)
        {
            var temp = items[firstIndex];
            
            items[firstIndex]  = items[secondIndex];
            items[secondIndex] = temp;
        }
        
        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentException(nameof(item));
            
            if (head - 1 < 0 || tail + 1 >= items.Length)
                Resize(items.Length * 2);
            
            // Adding these notes here just to remember this myself, comparison results:
            //     - less than 0 when lhs is le than rhs
            //     - 0 when lhs equals rhs
            //     - greater than 0 when lhs is gt than rhs
            
            // Check if we can do best case insertion by inserting to head or tail directly. If 
            // this fails we need to start scanning from head and tail to origin to find the best fit.
            if (comparer(item, items[tail - 1]) > 0 || items[tail] == null || tail == origin)
            {
                // Greater than current tail or initial insert.
                items[tail++] = item;
            }
            else if (comparer(item, items[head]) < 0)
            {
                // Smaller than current head, make new head.
                items[--head] = item;
            }
            else
            {
                var headIndex = head;
                var tailIndex = tail - 1;
                
                for (;;)
                {
                    // Scan head and insert if match.
                    if (headIndex <= origin && comparer(item, items[headIndex]) <= 0)
                    {
                        // Shift empty value from next head index to position where we insert next value. 
                        for (var i = head - 1; i < headIndex - 1; i++)
                            SwapItems(i, i + 1);
                        
                        // Insert new item.
                        items[headIndex - 1] = item;
                        
                        head--;
                        
                        break;
                    }

                    // Scan tail and insert if match.
                    if (tailIndex >= origin && comparer(item, items[tailIndex]) >= 0)
                    {
                        // Shift empty value from current tail index to position where we insert next value.
                        for (var i = tail; i > tailIndex + 1; i--)
                            SwapItems(i, i - 1);
                        
                        // Insert new item.
                        items[tailIndex + 1] = item;
                        
                        tail++;
                        
                        break;
                    }

                    if (headIndex < origin || tailIndex >= origin)
                    {
                        headIndex++;
                        tailIndex--;
                    }
                    else
                        throw new InvalidOperationException("could not find best fit for item");
                }
            }
        }
        
        public void AddRange(params T[] items) 
            => Array.ForEach(items, Add);

        public bool Remove(T item)
        {
            var headIndex = head;
            var tailIndex = tail - 1;
                
            for (;;)
            {
                // Scan head and remove if match.
                if (headIndex <= origin && items[headIndex].Equals(item))
                {
                    items[headIndex] = default;

                    // Shift value to be removed to be beginning of head.
                    for (var i = headIndex; i < head - 1; i++)
                        SwapItems(i, i - 1);
                        
                    head++;
                    
                    return true;
                }

                // Scan tail and remove if match.
                if (tailIndex >= origin && items[tailIndex].Equals(item))
                {
                    items[tailIndex] = default;
                    
                    // Shift value to be removed to the end of the tail.
                    for (var i = tailIndex; i < tail; i++)
                        SwapItems(i, i + 1);
                        
                    tail--;
                    
                    return true;
                }

                if (headIndex < origin || tailIndex >= origin)
                {
                    headIndex++;
                    tailIndex--;
                }
                else
                    return false;
            }
        }
        
        public bool Contains(T item)
        {
            for (var i = head; i < tail; i++)
            {
                if (items[i].Equals(item))
                    return true;
            }
            
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
            => Array.Copy(items, head, array, 0, Count);

        public IEnumerator<T> GetEnumerator()
            => new PriorityQueueEnumerator<T>(items, head, tail);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        
        public void Clear()
        {
            Array.Clear(items, 0, items.Length);
            
            origin = items.Length / 2;
            head   = origin;
            tail   = origin;
        }
    }
}