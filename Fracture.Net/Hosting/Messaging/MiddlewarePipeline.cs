using System;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting.Messaging
{
    public delegate void MiddlewareDelegate(in Request request, out bool pass);
    public delegate void MiddlewareDelegate<T>(in Request<T> request, out bool pass) where T : IMessage;
    
    public sealed class MiddlewareHandlerAttribute : Attribute
    {
        #region Properties
        public string Path
        {
            get;
        }
        #endregion

        public MiddlewareHandlerAttribute(string path)
            => Path = !string.IsNullOrEmpty(path) ? path : throw new ArgumentNullException(nameof(path));
    }
    
    public sealed class MiddlewarePipeline
    {
        public MiddlewarePipeline()
        {
        }
    }
}