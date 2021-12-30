using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Clients;
using Fracture.Net.Messages;

namespace Fracture.Engine.Net
{
    public delegate void NetMessageHandlerDelegate(IMessage message);
        
    public interface INetMessageHandler
    {
        void Use(MessageMatchDelegate match, NetMessageHandlerDelegate handler);
    }
    
    public class NetMessageHandler : INetMessageHandler
    {
        #region Private net message handler context
        private sealed class NetMessageHandlerContext
        {
            #region Properties
            public MessageMatchDelegate Match
            {
                get;
            }

            public NetMessageHandlerDelegate Handler
            {
                get;
            }
            #endregion

            public NetMessageHandlerContext(MessageMatchDelegate match, NetMessageHandlerDelegate handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion
        
        #region Fields
        private readonly List<NetMessageHandlerContext> contexts;
        #endregion

        public NetMessageHandler()
            => contexts = new List<NetMessageHandlerContext>();
        
        public void Use(MessageMatchDelegate match, NetMessageHandlerDelegate handler)
            => contexts.Add(new NetMessageHandlerContext(match, handler));
        
        public bool Handle(IMessage message)
        {
            var context = contexts.FirstOrDefault(c => c.Match(message));
            
            if (context == default)
                return false;
            
            context.Handler(message);
            
            return true;
        }
    }
}