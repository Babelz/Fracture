using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.ServiceModel.Syndication;
using Fracture.Common.Collections;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Net.Hosting.Messaging;
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
        /// Register custom optional kernel for use with the application. 
        /// </summary>
        IApplicationBuilder Kernel(Kernel kernel);
        
        /// <summary>
        /// Registers custom optional resource container for use with the application.
        /// </summary>
        IApplicationBuilder Resources(ApplicationResources resources);
        
        /// <summary>
        /// Register custom optional resources for use with the application.
        /// </summary>
        IApplicationBuilder Resources(IMessagePool messages = null,
                                      IArrayPool<byte> buffers = null,
                                      IPool<Request> requests = null,
                                      IPool<Response> responses = null,
                                      IPool<Notification> notifications = null);
        
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
        /// Register custom script manager for the application to use.
        /// </summary>
        IApplicationBuilder Scripts(IApplicationScriptManager scripts);
        
        /// <summary>
        /// Register custom service manager for the application to use.
        /// </summary>
        IApplicationBuilder Services(IApplicationServiceManager services);

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
    
    public readonly struct ApplicationBuilderBindingContext
    {
        #region Properties
        public Type Type
        {
            get;
        }

        public IBindingValue[] Args
        {
            get;
        }
        #endregion

        public ApplicationBuilderBindingContext(Type type, params IBindingValue[] args)
        {
            Type = type ?? throw new ArgumentException(nameof(type));
            Args = args;
        }
    }
    
    /// <summary>
    /// Default implementation of <see cref="IApplicationBuilder"/>.
    /// </summary>
    public sealed class ApplicationBuilder : IApplicationBuilder
    {
        #region Fields
        private readonly Queue<ApplicationBuilderBindingContext> startupScriptContexts;
        private readonly Queue<ApplicationBuilderBindingContext> serviceContexts;
        
        private readonly Queue<object> dependencies;
        
        private Kernel kernel;
        private ApplicationResources resources;
        
        private IRequestRouter router;
        private INotificationCenter notifications;
        
        private IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        private IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        
        private IApplicationScriptManager scripts;
        private IMessageSerializer serializer;
        private IApplicationTimer timer;
        
        private IApplicationServiceManager services;
        private IServer server;
        #endregion
        
        protected ApplicationBuilder()
        {
            dependencies          = new Queue<object>();
            startupScriptContexts = new Queue<ApplicationBuilderBindingContext>();
            serviceContexts       = new Queue<ApplicationBuilderBindingContext>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ApplicationResources CreateDefaultResources()
            => new ApplicationResources(
                new MessagePool(),
                new BlockArrayPool<byte>(new ArrayPool<byte>(() => new ListStorageObject<byte[]>(new List<byte[]>()), 0), 64, ushort.MaxValue),
                new CleanPool<Request>(new Pool<Request>(new LinearStorageObject<Request>(new LinearGrowthArray<Request>(256)))),
                new CleanPool<Response>(new Pool<Response>(new LinearStorageObject<Response>(new LinearGrowthArray<Response>(256)))),
                new CleanPool<Notification>(new Pool<Notification>(new LinearStorageObject<Notification>(new LinearGrowthArray<Notification>(256))))
            );

        public IApplicationBuilder Kernel(Kernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            
            return this;
        }

        public IApplicationBuilder Resources(ApplicationResources resources)
        {
            this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
            
            return this;
        }
        
        public IApplicationBuilder Resources(IMessagePool messages = null,
                                             IArrayPool<byte> buffers = null,
                                             IPool<Request> requests = null,
                                             IPool<Response> responses = null,
                                             IPool<Notification> notifications = null)
        {
            var defaultResources = CreateDefaultResources();
            
            resources = new ApplicationResources(
                messages ?? defaultResources.Messages,
                buffers ?? defaultResources.Buffers,
                requests ?? defaultResources.Requests,
                responses ?? defaultResources.Responses,
                notifications ?? defaultResources.Notifications
            );
            
            return this;
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

        public IApplicationBuilder Services(IApplicationServiceManager services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            
            return this;
        }

        public IApplicationBuilder Serializer(IMessageSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            
            return this;
        }
        
        public IApplicationBuilder Timer(IApplicationTimer timer)
        {
            this.timer = timer ?? throw new ArgumentNullException(nameof(timer));
            
            return this;
        }
        
        public IApplicationBuilder Scripts(IApplicationScriptManager scripts)
        {
            this.scripts = scripts ?? throw new ArgumentNullException(nameof(scripts));
            
            return this;
        }
        
        public IApplicationBuilder StartupScript<T>(params IBindingValue[] args)
        {
            startupScriptContexts.Enqueue(new ApplicationBuilderBindingContext(typeof(T), args));
            
            return this;
        }

        public IApplicationBuilder Service<T>(params IBindingValue[] args)
        {
            serviceContexts.Enqueue(new ApplicationBuilderBindingContext(typeof(T), args));
            
            return this;
        }
        
        public IApplicationBuilder Dependency(object dependency)
        {
            dependencies.Enqueue(dependency ?? throw new ArgumentNullException(nameof(dependency)));
            
            return this;
        }

        public IApplication Build()
        {
            if (server == null)
                throw new InvalidOperationException("server was not set");

            // Create and locate dependencies injected via builder methods.
            resources ??= CreateDefaultResources();

            kernel        ??= new Kernel(DependencyBindingOptions.Interfaces);
            router        ??= new RequestRouter();
            notifications ??= new NotificationCenter(resources.Notifications);
            
            requestMiddleware      ??= new MiddlewarePipeline<RequestMiddlewareContext>();
            responseMiddleware     ??= new MiddlewarePipeline<RequestResponseMiddlewareContext>();
            notificationMiddleware ??= new MiddlewarePipeline<NotificationMiddlewareContext>();
            
            scripts    ??= new ApplicationScriptManager();
            services   ??= new ApplicationServiceManager();
            serializer ??= new MessageSerializer(resources.Buffers);
            timer      ??= new ApplicationTimer();
            
            // Initialize application.
            var application = new Application(
                kernel,
                resources,
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
            
            // Register all dependencies.
            while (dependencies.Count != 0)
                kernel.Bind(dependencies.Dequeue());
            
            // Register all services.
            while (serviceContexts.Count != 0)
            {
                var context = serviceContexts.Dequeue();
                
                kernel.Bind(context.Type, context.Args);
            }
            
            // Register all startup scripts.
            while (startupScriptContexts.Count != 0)
            {
                var context = startupScriptContexts.Dequeue();
                
                scripts.Load(context.Type, context.Args);
            }
            
            return application;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationBuilder Create()
            => new ApplicationBuilder();
    }
}