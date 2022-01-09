using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Clients;

namespace Fracture.Engine.Net
{
    public delegate void NetPacketHandlerDelegate(ClientUpdate.Packet packet);

    public interface INetPacketHandler
    {
        void Use(string name, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler);
        
        void Revoke(string name);
    }
    
    public class NetPacketHandler : INetPacketHandler
    {
        #region Private net message handler context
        private sealed class NetMessageHandlerContext
        {
            #region Properties
            public string Name
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

            public NetMessageHandlerContext(string name, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            {
                Name    = name ?? throw new ArgumentNullException(nameof(name));
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
        
        public void Use(string name, NetPacketMatchDelegate match, NetPacketHandlerDelegate handler)
            => contexts.Add(new NetMessageHandlerContext(name, match, handler));
        
        public void Revoke(string name)
        {
            if (!contexts.Remove(contexts.FirstOrDefault(c => c.Name == name)))
                throw new InvalidOperationException($"no handle with name {name} exists"); 
        }
        
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