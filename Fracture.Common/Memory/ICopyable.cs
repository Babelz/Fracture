namespace Fracture.Common.Memory
{
    /// <summary>
    /// Interface for implementing generic copy operations.
    /// </summary>
    public interface ICopyable<in T>
    {
        /// <summary>
        /// Copy other objects state to this object.
        /// </summary>
        void Copy(T from);
    }
}
