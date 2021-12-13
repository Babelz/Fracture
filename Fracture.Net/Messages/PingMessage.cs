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
        /// Gets or sets the request time for this message. Contains the time this message was created.
        /// </summary>
        public TimeSpan RequestTime
        {
            get;
            set;
        }
        #endregion

        public PingMessage()
        {
        }

        public override void Clear()
            => RequestTime = default;
    }
}