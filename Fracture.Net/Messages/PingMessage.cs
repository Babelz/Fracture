using System;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Simple message that is used for testing connection status.
    /// </summary>
    public sealed class PingMessage : Message
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
        #endregion

        public PingMessage()
        {
        }

        public override void Clear()
            => RequestId = default;
    }
}