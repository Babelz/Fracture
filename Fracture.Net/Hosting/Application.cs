using System;
using System.Net;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Scripting;

namespace Fracture.Net.Hosting
{
    public interface IApplicationHost
    {
        #region Events
        event EventHandler Starting;
        
        event EventHandler ShuttingDown;
        #endregion

        void Shutdown();
    }
    
    public sealed class ApplicationRequestContext
    {
        #region Properties
        IRequestRouter Router
        {
            get;
        }
        
        IMiddlewareConsumer<RequestMiddlewareContext> Middleware
        {
            get;
        }
        #endregion
        
        public ApplicationRequestContext(IRequestRouter router, IMiddlewareConsumer<RequestMiddlewareContext> middleware)
        {
            Router     = router ?? throw new ArgumentNullException(nameof(router));
            Middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
        }
    }
    
    public sealed class ApplicationNotificationContext
    {
        #region Properties
        INotificationQueue Queue
        {
            get;
        }
        
        IMiddlewareConsumer<NotificationMiddlewareContext> Middleware
        {
            get;
        }
        #endregion

        public ApplicationNotificationContext(INotificationQueue queue, IMiddlewareConsumer<NotificationMiddlewareContext> middleware)
        {
            Queue      = queue ?? throw new ArgumentNullException(nameof(queue));
            Middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
        }
    }
    
    public interface IApplicationMessagingHost : IApplicationHost
    {
        #region Properties
        ApplicationRequestContext Requests
        {
            get;
        }

        ApplicationNotificationContext Notifications
        {
            get;
        }
        
        IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses
        {
            get;
        }
        #endregion
    }
    
    public interface IApplicationScriptingHost : IApplicationMessagingHost
    {
        #region Properties
        public IScriptHost Scripts
        {
            get;
        }
        #endregion
    }

    public interface IApplication : IApplicationHost
    {
        void Start();
    }
}