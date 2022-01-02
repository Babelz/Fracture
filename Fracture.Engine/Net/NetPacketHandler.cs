using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Clients;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate void NetPacketHandlerDelegate(ClientUpdate.Packet packet);
        
    public interface INetPacketHandler
    {
        void Use(NetPacketMatchDelegate match, NetPacketHandlerDelegate handler);
    }
    
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
        private readonly List<NetMessageHandlerContext> contexts;
        #endregion

        public NetPacketHandler()
            => contexts = new List<NetMessageHandlerContext>();
        
        public void Use(NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            => contexts.Add(new NetMessageHandlerContext(match, handler));
        
        public bool Handle(ClientUpdate.Packet packet)
        {
            var context = contexts.FirstOrDefault(c => c.Match(packet));
            
            if (context == default)
                return false;
            
            context.Handler(packet);
            
            return true;
        }
    }
}