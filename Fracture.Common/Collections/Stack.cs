using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Fracture.Common.Collections
{
    /// <summary>
    /// Interface for creating generic stacks.
    /// </summary>
    public interface IStack<T>
    {
        #region Properties
        /// <summary>
        /// Returns boolean declaring whether the stack is empty.
        /// </summary>
        bool Empty
        {
            get;
        }

        /// <summary>
        /// Returns the current stack pointer position. This 
        /// always points to the position of next element of the stack.
        /// </summary>
        int Top
        {
            get;
        }
        #endregion

        /// <summary>
        /// Peeks the stack, returns the top value.
        /// </summary>
        T Peek();

        /// <summary>
        /// Removes and returns the top value from the stack.
        /// </summary>
        T Pop();

        /// <summary>
        /// Push new value on top of the stack.
        /// </summary>
        void Push(T value);
    }

    /// <summary>
    /// Generic stack that grows in linear manner using linear growth arrays for storage. 
    /// </summary>
    public sealed class LinearGrowthStack<T> : IStack<T>
    {
        #region Fields
        /// <summary>
        /// Storage for stack elements.
        /// </summary>
        private readonly LinearGrowthArray<T> storage;
        #endregion

        #region Properties
        public bool Empty => Top == 0;

        public int Top
        {
            get;
            private set;
        }
        #endregion

        public LinearGrowthStack(LinearGrowthArray<T> storage) => this.storage = storage ?? throw new ArgumentNullException(nameof(storage));

        /// <summary>
        /// Creates new instance of <see cref="LinearGrowthStack{T}"/> using default storage
        /// that has bucket (page) size of 8 elements and bucket (page) count of 1.
        /// </summary>
        public LinearGrowthStack()
            : this(new LinearGrowthArray<T>(8))
        {
        }

        public T Peek() => !Empty ? storage.AtIndex(Top - 1) : throw new InvalidOperationException("stack is empty");

        public T Pop()
        {
            if (Empty)
                throw new InvalidOperationException("stack is empty");

            var element = storage.AtIndex(--Top);

            // Clear the value or reference to allow GC collection.
            storage.Insert(Top, default);

            return element;
        }

        public void Push(T value)
        {
            if (Top >= storage.Length) storage.Grow();

            storage.Insert(Top++, value);
        }
    }

    /// <summary>
    /// Generic stack that grows in linear manner and keeps track of elements added  and removed. Adding duplicated
    /// element causes and exception to be throw. Useful for debugging but has a hefty performance penalty when using.
    /// </summary>
    public sealed class UniqueLinearGrowthStack<T> : IStack<T>
    {
        #region Fields
        // Stack trace of items that were pushed on the stack.
        private readonly Dictionary<T, StackFrame> pushers;

        // Stack trace of items that were popped from the stack.
        private readonly Dictionary<T, StackFrame> poppers;

        // Lookup containing all on the stack.
        private readonly HashSet<T> lookup;

        private readonly IStack<T> stack;
        #endregion

        #region Properties
        public bool Empty => stack.Empty;
        public int Top => stack.Top;
        #endregion

        public UniqueLinearGrowthStack(IStack<T> stack)
        {
            this.stack = stack ?? throw new ArgumentNullException(nameof(stack));

            lookup  = new HashSet<T>();
            pushers = new Dictionary<T, StackFrame>();
            poppers = new Dictionary<T, StackFrame>();
        }

        public UniqueLinearGrowthStack()
            : this(new LinearGrowthStack<T>())
        {
        }

        public T Peek() => stack.Peek();

        /// <summary>
        /// Pops top value from the stack. If the value being popped
        /// is duplicated on the stack, causes an exception.
        /// </summary>
        public T Pop()
        {
            // Get the item.
            var item = stack.Pop();

            // Remove from lookup.
            lookup.Remove(item);

            // Remove from push log.
            if (pushers.ContainsKey(item))
                pushers.Remove(item);

            // Duplicated item if the item has been popped already.
            if (poppers.ContainsKey(item))
                throw new InvalidOperationException("can't pop element from stack, stack is not unique");

            // All ok, log the pop operation for future checks.
            poppers[item] = new StackFrame();

            return item;
        }

        /// <summary>
        /// Pushes given value to the stack. Throws an exception in case the 
        /// value already exists on the stack.
        /// </summary>
        public void Push(T value)
        {
            if (!lookup.Add(value))
            {
                // Exists on the stack, duplicated entry. 
                // Print stack trace from popper and pusher of the item.
                var sb = new StringBuilder();

                pushers.TryGetValue(value, out var pusher);
                poppers.TryGetValue(value, out var popper);

                sb.Append("can't add element to the stack, adding the element would break the uniqueness of elements\n\n");
                sb.Append($"first push calls stack trace: {pusher}\n\n");
                sb.Append($"first pop calls stack trace: {popper}\n");

                throw new InvalidOperationException(sb.ToString());
            }

            // All ok, log push and continue.
            if (poppers.ContainsKey(value))
                poppers.Remove(value);

            pushers[value] = new StackFrame();

            stack.Push(value);
        }
    }
}