namespace Fracture.Net
{
    /// <summary>
    /// Enumeration defining all possible reasons why connection was reset.
    /// </summary>
    public enum ResetReason : byte
    {
        /// <summary>
        /// Local entity has reset the connection and disconnected it.
        /// </summary>
        LocalReset = 0,
        
        /// <summary>
        /// Remote entity has reset the connection.
        /// </summary>
        RemoteReset,
        
        /// <summary>
        /// Connection was reset because it has timed out.
        /// </summary>
        TimedOut
    }
}