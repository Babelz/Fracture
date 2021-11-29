using System;
using System.Collections.Generic;
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
        private readonly Queue<BindingContext> serviceContexts;
        private readonly Queue<BindingContext> scriptContexts;
        
        private readonly Queue<object> dependencies;
        
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
            serviceContexts = new Queue<BindingContext>();
            scriptContexts  = new Queue<BindingContext>();
            
            dependencies = new Queue<object>();
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
        public ApplicationBuilder Service<T>(params IBindingValue[] args)
        {
            serviceContexts.Enqueue(new BindingContext(typeof(T), args));
            
            return this;
        }
        
        /// <summary>
        /// Register startup script that is loaded to the application before it starts.
        /// </summary>
        public ApplicationBuilder Script<T>(params IBindingValue[] args)
        {
            scriptContexts.Enqueue(new BindingContext(typeof(T), args));
            
            return this;
        }
        
        /// <summary>
        /// Register custom dependency for the application that the services and scripts can use.
        /// </summary>
        public ApplicationBuilder Dependency(object dependency)
        {
            dependencies.Enqueue(dependency);
            
            return this;
        }

        /// <summary>
        /// Builds the application using dependencies and configurations provided. To start running the application call the <see cref="IApplication.Start"/>
        /// method.
        /// </summary>
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

            var scriptKernel  = new Kernel(DependencyBindingOptions.Interfaces);
            var serviceKernel = new Kernel(DependencyBindingOptions.Interfaces);
            
            var scripts  = new ApplicationScriptManager(scriptKernel);
            var services = new ApplicationServiceManager(serviceKernel);
            
            // Initialize application.
            var application = new Application(
                server,
                router,
                notifications,
                requestMiddleware,
                notificationMiddleware,
                responseMiddleware,
                scripts,
                services,
                serializer,
                timer
            );
            
            // Register all custom dependencies to both kernels.
            while (dependencies.Count != 0)
            {
                var dependency = dependencies.Dequeue();
                
                serviceKernel.Bind(dependency);
                scriptKernel.Bind(dependency);
            }
            
            // Register application to kernel as dependency for services and scripts to locate it.
            serviceKernel.Proxy(application, typeof(IApplicationServiceHost));
            scriptKernel.Proxy(application, typeof(IApplicationScriptingHost));
            
            // Register services to service kernel.
            while (serviceContexts.Count != 0)
            {
                var context = serviceContexts.Dequeue();
                
                serviceKernel.Bind(context.Type, context.Args);
            }

            // Cook all service dependencies and inject them to script kernel for scripts to use.
            foreach (var service in serviceKernel.All<IApplicationService>())
                scriptKernel.Bind(service);
            
            // Load all startup scripts.
            while (scriptContexts.Count != 0)
            {
                var context = scriptContexts.Dequeue();
                
                scripts.Load(context.Type, context.Args);
            }
            
            return application;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationBuilder Create()
            => new ApplicationBuilder();
    }
}