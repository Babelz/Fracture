using System;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    public delegate void MiddlewareDelegate(in Request request, out bool pass);
    public delegate void MiddlewareDelegate<T>(in Request<T> request, out bool pass) where T : IMessage;
    
    public sealed class MiddlewareHandlerAttribute : Attribute
    {
        #region Properties
        public Type Path
        {
            get;
        }
        #endregion

        public MiddlewareHandlerAttribute(Type path)
            => Path = path ?? throw new ArgumentNullException(nameof(path));
    }
    
    public sealed class MiddlewarePipeline
    {
        public MiddlewarePipeline()
        {
        }
    }
}