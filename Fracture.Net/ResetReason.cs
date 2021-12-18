namespace Fracture.Net
{
    /// <summary>
    /// Enumeration defining all possible reasons why connection was reset.
    /// </summary>
    public enum ResetReason : byte
    {
        /// <summary>
        /// Server has reset the peer and disconnected it.
        /// </summary>
        ServerReset = 0,
        
        /// <summary>
        /// Remote client has reset the peer by closing the connection.
        /// </summary>
        RemoteReset,
        
        /// <summary>
        /// Connection was reset because it has timed out.
        /// </summary>
        TimedOut
    }
}