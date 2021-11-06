using System.Net.Sockets;

namespace Fracture.Net.Hosting.Peers
{
    /// <summary>
    /// Interface for implementing peer factories.
    /// </summary>
    public interface IPeerFactory
    {
        /// <summary>
        /// Creates new peer using given socket connected by remote client.
        /// </summary>
        IPeer Create(Socket socket);
    }
}