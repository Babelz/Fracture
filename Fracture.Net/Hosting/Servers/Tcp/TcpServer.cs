using System;

namespace Fracture.Net.Hosting.Servers.Tcp
{
    /// <summary>
    /// Class that provides TCP server functionality using <see cref="TcpListener"/> and <see cref="TcpPeerFactory"/>.
    /// </summary>
    public sealed class TcpServer : Server
    {
        public TcpServer(TimeSpan gracePeriod, int port, int backlog = 0)
            : base(new TcpPeerFactory(gracePeriod), new TcpListener(port, backlog))
        {
        }
    }
}