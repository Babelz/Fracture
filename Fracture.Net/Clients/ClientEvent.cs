using System;
using System.Security.Cryptography.X509Certificates;

namespace Fracture.Net.Clients
{
    public abstract class ClientEvent
    {
        #region Properties
        public TimeSpan Timestamp
        {
            get;
        }
        #endregion
        
        protected ClientEvent()
        {
        }
        
        public sealed class Connected
        {
        }

        public sealed class Disconnected
        {
        }

        public sealed class ConnectFailed
        {
        }

        public sealed class IncomingMessage
        {
        }

        public sealed class OutgoingMessage
        {
        }
    }
}