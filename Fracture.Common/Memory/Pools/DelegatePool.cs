using System;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Pool that uses user provided delegate call back for creating new elements.
    /// </summary>
    public sealed class DelegatePool<T> : PoolBase<T> where T : class
    {
        #region Properties
        private readonly Func<T> instantiate;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="DelegatePool{T}"/> with given storage, allocation function
        /// and initial objects. Can be provided with capacity to limit the count of pooled objects.
        /// </summary>
        public DelegatePool(IStorageObject<T> storage, Func<T> instantiate, int initialStoredObjectsCount, int capacity)
            : base(storage, initialStoredObjectsCount, capacity)

        {
            this.instantiate = instantiate ?? throw new ArgumentNullException(nameof(instantiate));

            CreateObjects(initialStoredObjectsCount);
        }

        public DelegatePool(IStorageObject<T> storage, Func<T> newObject, int initialStoredObjectsCount)
            : this(storage, newObject, initialStoredObjectsCount, 0)
        {
        }

        public DelegatePool(IStorageObject<T> storage, Func<T> newObject)
            : this(storage, newObject, 0, 0)
        {
        }

        protected override T New() => instantiate();
    }
}