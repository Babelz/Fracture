using System;
using System.Net;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Services;

namespace Fracture.Net.Hosting
{
    public interface IApplicationHost
    {
        #region Events
        event EventHandler Starting;
        event EventHandler ShuttingDown;
        #endregion

        #region Properties
        IRequestRouter RequestRouter
        {
            get;
        }
        
        INotificationQueue NotificationQueue
        {
            get;
        }
        
        IMiddlewareConsumer<RequestMiddlewareContext> RequestMiddleware
        {
            get;
        }
        
        IMiddlewareConsumer<RequestResponseMiddlewareContext> ResponseMiddleware
        {
            get;
        }
        
        IMiddlewareConsumer<NotificationMiddlewareContext> NotificationMiddleware
        {
            get;
        }
        #endregion
        
        void Shutdown();
    }
    
    public interface IApplicationScriptingHost : IApplicationHost
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