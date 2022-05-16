namespace Fracture.Common.Memory
{
    /// <summary>
    /// Interface for implementing generic cloning operations.
    /// </summary>
    public interface ICloneable<out T>
    {
        /// <summary>
        /// Returns copy of this object. This interface does not determine whether the copy is shallow or deep.
        /// </summary>
        T Clone();
    }
}