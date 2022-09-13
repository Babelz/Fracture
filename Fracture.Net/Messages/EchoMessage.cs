namespace Fracture.Net.Messages
{
    /// <summary>
    /// Enumeration that defines echo phase.
    /// </summary>
    public enum EchoPhase : byte
    {
        /// <summary>
        /// Ping phase, pong is to be expected after this phase.
        /// </summary>
        Ping = 0,

        /// <summary>
        /// Pong phase, expected to happen after ping phase.
        /// </summary>
        Pong
    }

    /// <summary>
    /// Simple message that is used for testing connection status.
    /// </summary>
    public sealed class EchoMessage : Message
    {
        #region Properties
        /// <summary>
        /// Gets or sets the request id used for identifying ping messages between communicating parties. 
        /// </summary>
        public ulong RequestId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the phase.
        /// </summary>
        public EchoPhase Phase
        {
            get;
            set;
        }
        #endregion

        public EchoMessage()
        {
        }

        public override void Clear()
        {
            RequestId = default;
            Phase     = default;
        }
    }
}