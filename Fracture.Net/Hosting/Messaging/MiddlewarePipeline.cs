using System;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    public delegate void MiddlewareDelegate(in Request request, out bool pass);

    public interface IMiddlewareConsumer
    {
        void Use(MessageMatchDelegate match, MiddlewareDelegate middleware);
    }
    
    public interface IMiddlewareHandler
    {
        void Handle(in Request request, out bool pass);
    }
    
    public sealed class MiddlewarePipeline : IMiddlewareConsumer, IMiddlewareHandler
    {
        public MiddlewarePipeline()
        {
        }

        public void Use(MessageMatchDelegate match, MiddlewareDelegate middleware)
        {
            throw new NotImplementedException();
        }

        public void Handle(in Request request, out bool pass)
        {
            throw new NotImplementedException();
        }
    }
}