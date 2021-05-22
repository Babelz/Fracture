namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Delegate for wrapping serialization functions.
    /// </summary>
    public delegate void Serialize<in T>(T value, byte[] buffer, int offset);
    
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate T Deserialize<out T>(byte[] buffer, int offset);
    
    public static class ObjectSerializationGenerator
    {
    }
}