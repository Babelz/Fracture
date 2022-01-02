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
    
    public interface IManagedNetPacketHandler
    { 
        void Use(object consumer, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler);
        
        bool Clear(object consumer);
    }
    
    public class ManagedNetPacketHandler : IManagedNetPacketHandler
    {
        #region Private net message handler context
        private sealed class NetMessageHandlerContext
        {
            #region Properties
            public object Consumer
            {
                get;
            }
            
            public NetPacketMatchDelegate Match
            {
                get;
            }

            public NetPacketHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public NetMessageHandlerContext(object consumer, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            {
                Consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
                Match    = match ?? throw new ArgumentNullException(nameof(match));
                Handler  = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion
        
        #region Fields
        private readonly List<NetMessageHandlerContext> contexts;
        #endregion

        public ManagedNetPacketHandler()
            => contexts = new List<NetMessageHandlerContext>();
        
        public void Use(object consumer, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            => contexts.Add(new NetMessageHandlerContext(consumer, match, handler));
        
        public bool Clear(object consumer)
            => contexts.RemoveAll(c => c.Consumer == consumer) != 0;
            
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