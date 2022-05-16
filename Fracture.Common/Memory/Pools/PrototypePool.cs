using System;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Memory.Pools
{
    /// <summary>
    /// Pool that uses prototype object for instantiating new objects.
    /// </summary>
    public sealed class PrototypePool<T> : PoolBase<T> where T : class, ICloneable<T>
    {
        #region Fields
        private readonly ICloneable<T> prototype;
        #endregion

        public PrototypePool(IStorageObject<T> storage, ICloneable<T> prototype, int initialStoredObjectsCount, int capacity)
            : base(storage, initialStoredObjectsCount, capacity)
        {
            this.prototype = prototype ?? throw new ArgumentNullException(nameof(prototype));

            CreateObjects(initialStoredObjectsCount);
        }

        public PrototypePool(IStorageObject<T> storage, ICloneable<T> prototype, int initialStoredObjectsCount)
            : this(storage, prototype, initialStoredObjectsCount, 0)
        {
        }

        public PrototypePool(IStorageObject<T> storage, ICloneable<T> prototype)
            : this(storage, prototype, 0, 0)
        {
        }

        protected override T New() => prototype.Clone();
    }
}