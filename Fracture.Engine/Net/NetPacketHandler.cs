using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Clients;

namespace Fracture.Engine.Net
{
    public delegate void NetPacketHandlerDelegate(ClientUpdate.Packet packet);

    /// <summary>
    /// Interface for implementing net packet handlers that provide handling functionality for packets.
    /// </summary>
    public interface INetPacketHandler
    {
        /// <summary>
        /// Creates new packet handler and bounds it to given handle object. Handler will be invoked for all packages that match the supplied match delegate. 
        /// </summary>
        void Use(object handle, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler);

        /// <summary>
        /// Revokes given handle objects all handlers and removes them.
        /// </summary>
        void Revoke(object handle);
    }

    /// <summary>
    /// Default implementation of <see cref="INetPacketHandler"/>.
    /// </summary>
    public class NetPacketHandler : INetPacketHandler
    {
        #region Private net message handler context
        private sealed class NetMessageHandlerContext
        {
            #region Properties
            public NetPacketMatchDelegate Match
            {
                get;
            }

            public NetPacketHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public NetMessageHandlerContext(NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion

        #region Fields
        private readonly Dictionary<object, List<NetMessageHandlerContext>> lookup;
        #endregion

        public NetPacketHandler()
            => lookup = new Dictionary<object, List<NetMessageHandlerContext>>();

        public void Use(object handle, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
        {
            if (handle == null)
                throw new ArgumentNullException(nameof(handle));
            
            if (!lookup.TryGetValue(handle, out var handlers))
            {
                handlers = new List<NetMessageHandlerContext>();
                
                lookup.Add(handle, handlers);
            }
            
            handlers.Add(new NetMessageHandlerContext(match, handler));
        }
        
        public void Revoke(object handle)
        {
            if (!lookup.Remove(handle))
                throw new InvalidOperationException($"no contexts exists for handle {handle}");
        }

        public bool Handle(ClientUpdate.Packet packet)
        {
            foreach (var contexts in lookup.Values)
            {
                foreach (var context in contexts)
                {
                    if (!context.Match(packet))
                        continue;

                    context.Handler(packet);

                    return true;
                }
            }

            return false;
        }
    }
}