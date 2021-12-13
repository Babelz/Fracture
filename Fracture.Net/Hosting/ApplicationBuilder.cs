using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Class that provides application building functionality.
    /// </summary>
    public sealed class ApplicationBuilder
    {
        #region Fields
        private readonly Kernel binder;
        #endregion
        
        private ApplicationBuilder(IServer server)
        {
            binder = new Kernel();
            
            binder.Bind(server ?? throw new ArgumentNullException(nameof(server)));
        }

        /// <summary>
        /// Registers custom request router for application.
        /// </summary>
        public ApplicationBuilder Router(IRequestRouter router)
        {
            binder.Bind(router ?? throw new ArgumentNullException(nameof(router)));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom notification center for application.
        /// </summary>
        public ApplicationBuilder Notifications(INotificationCenter notifications)
        {
            binder.Bind(notifications ?? throw new ArgumentNullException(nameof(notifications)));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom request middleware for application.
        /// </summary>
        public ApplicationBuilder RequestMiddleware(IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware)
        {
            binder.Bind(requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware)));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom response middleware for application. 
        /// </summary>
        public ApplicationBuilder ResponseMiddleware(IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware)
        {
            binder.Bind(responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware)));
            
            return this;
        }
        
        /// <summary>
        /// Registers custom notification middleware for application. 
        /// </summary>
        public ApplicationBuilder NotificationMiddleware(IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware)
        {
            binder.Bind(notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware)));
            
            return this;
        }

        /// <summary>
        /// Register custom application timer for the application to use.
        /// </summary>
        public ApplicationBuilder Timer(IApplicationTimer timer)
        {
            binder.Bind(timer ?? throw new ArgumentNullException(nameof(timer)));
            
            return this;
        }
        
        /// <summary>
        /// Register custom message serializer for the application to use.
        /// </summary>
        public ApplicationBuilder Serializer(IMessageSerializer serializer)
        {
            binder.Bind(serializer ?? throw new ArgumentNullException(nameof(serializer)));
            
            return this;
        }

        /// <summary>
        /// Builds the application using dependencies and configurations provided. To start running the application call the <see cref="Application.Start"/>
        /// method.
        /// </summary>
        public Application Build()
        {
            // Check application bindings and bind default values if any are missing.
            if (!binder.Exists<IRequestRouter>()) 
                binder.Bind<RequestRouter>();
            
            if (!binder.Exists<INotificationCenter>()) 
                binder.Bind<NotificationCenter>();
            
            if (!binder.Exists<IMiddlewarePipeline<RequestMiddlewareContext>>()) 
                binder.Bind<MiddlewarePipeline<RequestMiddlewareContext>>();
            
            if (!binder.Exists<IMiddlewarePipeline<NotificationMiddlewareContext>>()) 
                binder.Bind<MiddlewarePipeline<NotificationMiddlewareContext>>();
            
            if (!binder.Exists<IMiddlewarePipeline<RequestResponseMiddlewareContext>>()) 
                binder.Bind<MiddlewarePipeline<RequestResponseMiddlewareContext>>();
            
            if (!binder.Exists<IMessageSerializer>()) 
                binder.Bind<MessageSerializer>();
            
            if (!binder.Exists<IApplicationTimer>()) 
                binder.Bind<ApplicationTimer>();
            
            // Initialize application.
            return binder.Activate<Application>();
        }
        
        /// <summary>
        /// Register the server for use by the application. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ApplicationBuilder FromServer(IServer server)
            => new ApplicationBuilder(server);
    }
}