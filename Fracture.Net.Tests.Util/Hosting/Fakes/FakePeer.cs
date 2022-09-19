using System.Net;
using System.Runtime.CompilerServices;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Tests.Util.Hosting.Fakes
{
    /// <summary>
    /// Static utility class that provides easy way to interact and create peers for testing purposes.
    /// </summary>
    public static class FakePeer
    {
        #region Fields
        // Static field for generating IP addresses.
        private static long ip;

        // Static field for generating peer identifiers.
        private static int id;
        #endregion

        /// <summary>
        /// Creates new peer with unique ID and IP address.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PeerConnection Create()
            => new PeerConnection(id++, new IPEndPoint(ip++, FakeServer.Port));
    }
}