using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Scripting;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface for implementing application builders. Builder should provide default implementations for each builder method that is used for injecting
    /// custom implementations.
    /// </summary>
    public interface IApplicationBuilder
    {
        /// <summary>
        /// Registers custom optional resource container for use with the application.
        /// </summary>
        IApplicationBuilder Resources(ApplicationResources resources);
        
        /// <summary>
        /// Registers custom request router for application.
        /// </summary>
        IApplicationBuilder Router(IRequestRouter router);
        
        /// <summary>
        /// Registers custom notification center for application.
        /// </summary>
        IApplicationBuilder Notifications(INotificationCenter notifications);
        
        /// <summary>
        /// Registers custom request middleware for application.
        /// </summary>
        IApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> middleware);
        
        /// <summary>
        /// Registers custom response middleware for application. 
        /// </summary>
        IApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> middleware);
        
        /// <summary>
        /// Registers custom notification middleware for application. 
        /// </summary>
        IApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> middleware);
        
        /// <summary>
        /// Register the server for use by the application. 
        /// </summary>
        IApplicationBuilder Server(IServer server);
        
        /// <summary>
        /// Register custom script manager for the application to use.
        /// </summary>
        IApplicationBuilder Scripts(IScriptManager scripts);
        
        /// <summary>
        /// Register custom message serializer for the application to use.
        /// </summary>
        IApplicationBuilder Serializer(IMessageSerializer serializer);
        
        /// <summary>
        /// Register custom application timer for the application to use.
        /// </summary>
        IApplicationBuilder Timer(IApplicationTimer timer);
        
        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        IApplicationBuilder StartupScript<T>(params IBindingValue[] args);
        
        /// <summary>
        /// Register service to be used by the application.
        /// </summary>
        IApplicationBuilder Service<T>(params IBindingValue[] args);
        
        /// <summary>
        /// Register custom dependency for the application that the services and scripts can use.
        /// </summary>
        IApplicationBuilder Dependency(object dependency);
        
        /// <summary>
        /// Builds the application using dependencies and configurations provided.
        /// </summary>
        IApplication Build();
    }
    
    /// <summary>
    /// Default implementation of <see cref="IApplicationBuilder"/>.
    /// </summary>
    public sealed class ApplicationBuilder : IApplicationBuilder
    {
        #region Fields
        private readonly Kernel kernel;
        #endregion
        
        protected ApplicationBuilder()
            => kernel = new Kernel(DependencyBindingOptions.ClassesInterfaces);
        
        public IApplicationBuilder Resources(ApplicationResources resources)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Router(IRequestRouter router)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Notifications(INotificationCenter notifications)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> middleware)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> middleware)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> middleware)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Server(IServer server)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Scripts(IScriptManager scripts)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Serializer(IMessageSerializer serializer)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Timer(IApplicationTimer timer)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder StartupScript<T>(params IBindingValue[] args)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Service<T>(params IBindingValue[] args)
        {
            throw new System.NotImplementedException();
        }

        public IApplicationBuilder Dependency(object dependency)
        {
            throw new System.NotImplementedException();
        }

        public IApplication Build()
        {
            throw new System.NotImplementedException();
        }
        
        public static ApplicationBuilder Create()
            => new ApplicationBuilder();
    }
}