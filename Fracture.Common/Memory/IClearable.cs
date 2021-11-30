namespace Fracture.Common.Memory
{
    /// <summary>
    /// Interface for implementing object state clearing operations.
    /// </summary>
    public interface IClearable
    {
        /// <summary>
        /// Clears the objects state to its initial state.
        /// </summary>
        void Clear();
    }
}
