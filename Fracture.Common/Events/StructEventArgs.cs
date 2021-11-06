namespace Fracture.Common.Events
{
    /// <summary>
    /// Interface for marking structure types used for struct event handlers.
    /// </summary>
    public interface IStructEventArgs
    {
        // Marker interface, nothing to implement.
    }
    
    /// <summary>
    /// Default delegate type for creating structure based event handlers.
    /// </summary>
    public delegate void StructEventHandler<T>(object sender, in T e) where T : struct, IStructEventArgs;
}