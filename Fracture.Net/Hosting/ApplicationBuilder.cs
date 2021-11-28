using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface for implementing application builders. Builder should provide default implementations for each builder method that is used for injecting
    /// custom implementations.
    /// </summary>
    public interface IApplicationBuilder
    {
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
        IApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware);
        
        /// <summary>
        /// Registers custom response middleware for application. 
        /// </summary>
        IApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware);
        
        /// <summary>
        /// Registers custom notification middleware for application. 
        /// </summary>
        IApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware);
        
        /// <summary>
        /// Register the server for use by the application. 
        /// </summary>
        IApplicationBuilder Server(IServer server);

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
        /// Register custom message serializer for the application to use.
        /// </summary>
        IApplicationBuilder Serializer(IMessageSerializer serializer);

        /// <summary>
        /// Builds the application using dependencies and configurations provided.
        /// </summary>
        IApplication Build();
    }

    /// <summary>
    /// Default implementation of <see cref="IApplicationBuilder"/>.
    /// </summary>
    public class ApplicationBuilder : IApplicationBuilder
    {
        #region Fields
        private readonly List<StartupScriptContext> startupScripts;
        
        private readonly Kernel kernel;
        
        private IRequestRouter router;
        private INotificationCenter notifications;
        
        private IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        private IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        
        private IMessageSerializer serializer;
        private IApplicationTimer timer;
        
        private IServer server;
        #endregion
        
        protected ApplicationBuilder()
        {
            startupScripts = new List<StartupScriptContext>();
            
            kernel = new Kernel(DependencyBindingOptions.Interfaces);
        }

        public IApplicationBuilder Router(IRequestRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            
            return this;
        }
        
        public IApplicationBuilder Notifications(INotificationCenter notifications)
        {
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            
            return this;
        }
        
        public IApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware)
        {
            this.requestMiddleware = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            
            return this;
        }

        public IApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware)
        {
            this.responseMiddleware = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            return this;
        }
        
        public IApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware)
        {
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            
            return this;
        }

        public IApplicationBuilder Server(IServer server)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            
            return this;
        }
        
        public IApplicationBuilder Timer(IApplicationTimer timer)
        {
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            
            return this;
        }
        
        public IApplicationBuilder Service<T>(params IBindingValue[] args)
        {
            kernel.Bind<T>(args);
            
            return this;
        }
        
        public IApplicationBuilder StartupScript<T>(params IBindingValue[] args)
        {
            startupScripts.Add(new StartupScriptContext(typeof(T), args));
            
            return this;
        }
        
        public IApplicationBuilder Dependency(object dependency)
        {
            kernel.Bind(dependency);
            
            return this;
        }
        
        public IApplicationBuilder Serializer(IMessageSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            
            return this;
        }

        public IApplication Build()
        {
            if (server == null)
                throw new InvalidOperationException("server was not set");
            
            router                 ??= new RequestRouter();
            notifications          ??= new NotificationCenter();
            requestMiddleware      ??= new MiddlewarePipeline<RequestMiddlewareContext>();
            responseMiddleware     ??= new MiddlewarePipeline<RequestResponseMiddlewareContext>();
            notificationMiddleware ??= new MiddlewarePipeline<NotificationMiddlewareContext>();
            timer                  ??= new ApplicationTimer();
            serializer             ??= new MessageSerializer();

            var scripts  = new ApplicationScriptManager(startupScripts);
            var services = new ApplicationServiceManager();
            
            // Initialize application.
            var application = new Application(
                kernel,
                router,
                notifications,
                requestMiddleware,
                notificationMiddleware,
                responseMiddleware,
                server,
                scripts,
                services,
                serializer,
                timer
            );
            
            // Register application to kernel as dependency for services and scripts to locate it.
            kernel.Bind(application);

            return application;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationBuilder Create()
            => new ApplicationBuilder();
    }
}