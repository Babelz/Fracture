using System;
using Fracture.Net.Hosting.Messaging;

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
    
    public interface IApplicationServiceHost : IApplicationHost
    {
        
    }
    
    public interface IApplicationScriptingHost : IApplicationHost
    {
        
    }
    
    public interface IApplication : IApplicationHost
    {
        void Start();
    }
}