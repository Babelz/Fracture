using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Policy;
using Fracture.Common.Di;
using Fracture.Common.Events;
using Fracture.Common.Memory.Pools;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Hosting.Scripting;
using Fracture.Net.Hosting.Servers;

namespace Fracture.Net.Hosting
{
    public interface IApplicationHost
    {
        #region Events
        event EventHandler Starting;
        event EventHandler ShuttingDown;
        
        event StructEventHandler<PeerJoinEventArgs> Join;
        event StructEventHandler<PeerJoinEventArgs> Reset; 
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
    
    public abstract class Application : IApplication, IApplicationScriptingHost
    {
        #region Fields
        private readonly Kernel kernel;
        
        private readonly IMessageSerializer serializer;
        private readonly IServer server;
        
        private readonly IArrayPool<byte> buffers;
        private readonly IMessagePool messages;
        
        private readonly List<ServerMessageEventArgs> outgoingMessages;
        private readonly List<PeerMessageEventArgs> incomingMessages;
        
        private readonly HashSet<PeerConnection> disconnectedPeers;
        private readonly HashSet<PeerConnection> joinedPeers;
        
        private bool running;
        #endregion
        
        #region Events
        public event EventHandler Starting;
        public event EventHandler ShuttingDown;
        
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerJoinEventArgs> Reset;
        #endregion

        #region Properties
        public ApplicationRequestContext Requests
        {
            get;
        }

        public ApplicationNotificationContext Notifications
        {
            get;
        }

        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses
        {
            get;
        }

        public IScriptHost Scripts
        {
            get;
        }
        #endregion

        public Application(Kernel kernel, 
                           ApplicationRequestContext requests,
                           ApplicationNotificationContext notifications,
                           IMiddlewareConsumer<RequestResponseMiddlewareContext> responses,
                           IServer server,
                           IScriptHost scripts,
                           IMessageSerializer serializer,
                           IMessagePool messages,
                           IArrayPool<byte> buffers)
        {
            this.kernel     = kernel ?? throw new ArgumentNullException(nameof(kernel));
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.messages   = messages ?? throw new ArgumentNullException(nameof(messages));
            this.buffers    = buffers ?? throw new ArgumentNullException(nameof(buffers));
            
            Requests      = requests ?? throw new ArgumentNullException(nameof(requests));
            Notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            Responses     = responses ?? throw new ArgumentNullException(nameof(responses));
            Scripts       = scripts ?? throw new ArgumentNullException(nameof(scripts));
            
            outgoingMessages  = new List<ServerMessageEventArgs>();
            incomingMessages  = new List<PeerMessageEventArgs>();
            disconnectedPeers = new HashSet<PeerConnection>();
            joinedPeers       = new HashSet<PeerConnection>();
        }

        #region Event handlers
        private void ServerOnOutgoing(object sender, in ServerMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ServerOnIncoming(object sender, in PeerMessageEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ServerOnReset(object sender, in PeerResetEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ServerOnJoin(object sender, in PeerJoinEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion
        
        private void Initialize()
        {
            Starting?.Invoke(this, EventArgs.Empty);
               
            server.Join     += ServerOnJoin;
            server.Reset    += ServerOnReset;
            server.Incoming += ServerOnIncoming;    
            server.Outgoing += ServerOnOutgoing;
            
            running = true;
        }

        private void Deinitialize()
        {
            ShuttingDown?.Invoke(this, EventArgs.Empty);
        }
        
        private void Tick()
        {
            
        }
        
        public void Shutdown()
        {
            if (!running)
                throw new InvalidOperationException("already running");
            
            running = false;
        }

        public void Start()
        {
            if (running)
                throw new InvalidOperationException("already running");
            
            Initialize();

            while (running)
                Tick();
            
            Deinitialize();
        }
    }
}