namespace Fracture.Common.Memory.Storages
{
    /// <summary>
    /// Interface for abstracting storage objects. Storage objects
    /// are used to store objects for re-use, by using this interface,
    /// the underlying storage type is hidden.
    /// </summary>
    public interface IStorageObject<T> where T : class
    {
        #region Properties
        /// <summary>
        /// Returns boolean declaring whether the storage object is empty.
        /// </summary>
        bool Empty
        {
            get;
        }

        /// <summary>
        /// Returns the count of objects in this storage.
        /// </summary>
        int Count
        {
            get;
        }
        #endregion

        /// <summary>
        /// Takes object from the storage object.
        /// </summary>
        T Take();

        /// <summary>
        /// Returns element to the storage object.
        /// </summary>
        void Return(T element);
    }
}