using System;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    public delegate void MiddlewareDelegate<T>(in T value, out bool pass);

    public interface IMiddlewareConsumer<T>
    {
        void Use(MessageMatchDelegate match, MiddlewareDelegate<T> middleware);
    }
    
    public interface IMiddlewareHandler<T>
    {
        void Handle(in T value, out bool pass);
    }
    
    public sealed class MiddlewarePipeline<T> : IMiddlewareConsumer<T>, IMiddlewareHandler<T>
    {
        public MiddlewarePipeline()
        {
        }

        public void Use(MessageMatchDelegate match, MiddlewareDelegate<T> middleware)
        {
            throw new NotImplementedException();
        }

        public void Handle(in T value, out bool pass)
        {
            throw new NotImplementedException();
        }
    }
}