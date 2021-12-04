using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Class that provides interface for building applications. 
    /// </summary>
    public sealed class ApplicationBuilder
    {
        #region Fields
        private readonly Kernel scripts;
        private readonly Kernel services;
        
        private IRequestRouter router;
        private INotificationCenter notifications;
        
        private IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        private IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        
        private IMessageSerializer serializer;
        private IApplicationTimer timer;
        
        private IServer server;
        #endregion
        
        public ApplicationBuilder()
        {
            scripts  = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Interfaces);
            services = new Kernel(DependencyBindingOptions.Interfaces);
        }

        /// <summary>
        /// Registers custom request router for application.
        /// </summary>
        public ApplicationBuilder Router(IRequestRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom notification center for application.
        /// </summary>
        public ApplicationBuilder Notifications(INotificationCenter notifications)
        {
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom request middleware for application.
        /// </summary>
        public ApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware)
        {
            this.requestMiddleware = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom response middleware for application. 
        /// </summary>
        public ApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware)
        {
            this.responseMiddleware = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom notification middleware for application. 
        /// </summary>
        public ApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware)
        {
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            
            return this;
        }

        /// <summary>
        /// Register the server for use by the application. 
        /// </summary>
        public ApplicationBuilder Server(IServer server)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            
            return this;
        }
        
        /// <summary>
        /// Register custom application timer for the application to use.
        /// </summary>
        public ApplicationBuilder Timer(IApplicationTimer timer)
        {
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            
            return this;
        }
        
        /// <summary>
        /// Register custom message serializer for the application to use.
        /// </summary>
        public ApplicationBuilder Serializer(IMessageSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            
            return this;
        }
        
        /// <summary>
        /// Register service to be used by the application.
        /// </summary>
        public ApplicationBuilder Service<T>(params IBindingValue[] args) where T : class, IApplicationService
        {
            services.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        public ApplicationBuilder Script<T>(params IBindingValue[] args) where T : class, IApplicationScript
        {
            scripts.Bind<T>(args);
            
            return this;
        }
        
        /// <summary>
        /// Builds the application using dependencies and configurations provided. To start running the application call the <see cref="Application.Start"/>
        /// method.
        /// </summary>
        public Application Build()
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
            
            // Initialize application.
            var application = new Application(
                server,
                router,
                notifications,
                requestMiddleware,
                notificationMiddleware,
                responseMiddleware,
                serializer,
                timer
            );
            
            // Initialize application kernel.
            var kernel = new ApplicationKernel(application, services, scripts);
            
            application.Tick += delegate
            {
                kernel.Tick();
            };

            return application;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationBuilder Create()
            => new ApplicationBuilder();
    }
}