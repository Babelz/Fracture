using System;
using System.Collections.Generic;

namespace Fracture.Net.Hosting.Messaging
{
    /// <summary>
    /// Interface for implementing middleware request context objects.
    /// </summary>
    public interface IMiddlewareRequestContext
    {
        // Marker interface, nothing to implement.
    }

    /// <summary>
    /// Delegate for matching middleware request objects.
    /// </summary>
    public delegate bool MiddlewareMatchDelegate<T>(in T context) where T : IMiddlewareRequestContext;
    
    /// <summary>
    /// Delegate for handling middleware request objects.
    /// </summary>
    public delegate void MiddlewareHandlerDelegate<T>(in T context, out bool reject) where T : IMiddlewareRequestContext;

    /// <summary>
    /// Interface for implementing middleware consumers that are used for registering middleware callbacks.
    /// </summary>
    public interface IMiddlewareConsumer<T> where T : IMiddlewareRequestContext
    {
        /// <summary>
        /// Register given middleware to the pipeline. All middleware requests that match the given matcher will be handled by given middleware delegate.
        /// </summary>
        /// <param name="match">matcher that the middleware request must match in order to be passed to the handler</param>
        /// <param name="handler">handler invoked when middleware request matches the preceding matcher</param>
        void Use(MiddlewareMatchDelegate<T> match, MiddlewareHandlerDelegate<T> handler);
    }

    /// <summary>
    /// Interface for implementing middleware invokers that handle running the middleware pipeline for middleware requests.
    /// </summary>
    public interface IMiddlewareInvoker<T> where T : IMiddlewareRequestContext
    {
        /// <summary>
        /// Invoke middleware for given middleware request.
        /// </summary>
        /// <param name="context">context object containing that is the middleware request</param>
        /// <returns>boolean declaring whether the request was rejected by the middleware</returns>
        bool Invoke(in T context);
    }

    /// <summary>
    /// Class that provides full middleware pipeline implementation by functioning as middleware consumer and invoker.  
    /// </summary>
    public class MiddlewarePipeline<T> : IMiddlewareInvoker<T>, IMiddlewareConsumer<T> where T : IMiddlewareRequestContext 
    {
        #region Private middleware class
        /// <summary>
        /// Private context class containing single middleware.
        /// </summary>
        private sealed class Middleware
        {
            #region Properties
            public MiddlewareMatchDelegate<T> Match
            {
                get;
            }

            public MiddlewareHandlerDelegate<T> Handler
            {
                get;
            }
            #endregion

            public Middleware(MiddlewareMatchDelegate<T> match, MiddlewareHandlerDelegate<T> handler)
            {
                Match   = match ?? throw new ArgumentNullException(nameof(match));
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            }
        }
        #endregion

        #region Fields
        private readonly List<Middleware> middlewares;
        #endregion

        public MiddlewarePipeline()
            => middlewares = new List<Middleware>();

        public void Use(MiddlewareMatchDelegate<T> match, MiddlewareHandlerDelegate<T> handler)
            => middlewares.Add(new Middleware(match, handler));
        
        public bool Invoke(in T context)
        {
            // Go trough all middlewares that match given request.
            foreach (var middleware in middlewares)
            {
                // Only run middlewares that match.
                if (!middleware.Match(context))
                    continue;
                
                // Invoke the middleware that matched given request.
                middleware.Handler(context, out var reject);
                
                // Immediately return if the middleware rejected the request.
                if (reject)
                    return false;
            }
            
            return true;
        }
    }
}