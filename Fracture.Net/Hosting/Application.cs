using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Events;
using Fracture.Common.Memory.Pools;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Peers;
using Fracture.Net.Hosting.Scripting;
using Fracture.Net.Hosting.Servers;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using NLog;

namespace Fracture.Net.Hosting
{
    public interface IApplicationHost
    {
        #region Events
        event EventHandler Starting;
        event EventHandler ShuttingDown;
        #endregion

        #region Properties
        IApplicationClock Clock
        {
            get;
        }
        #endregion

        void Shutdown();
    }
    
    public sealed class ApplicationRequestContext
    {
        #region Properties
        public IRequestRouteConsumer RouteConsumer
        {
            get;
        }
        
        public IMiddlewareConsumer<RequestMiddlewareContext> Middleware
        {
            get;
        }
        #endregion
        
        public ApplicationRequestContext(IRequestRouteConsumer routeConsumer, IMiddlewareConsumer<RequestMiddlewareContext> middleware)
        {
            RouteConsumer     = routeConsumer;
            Middleware = middleware;
        }
    }
    
    public sealed class ApplicationNotificationContext
    {
        #region Properties
        public INotificationQueue Queue
        {
            get;
        }
        
        public IMiddlewareConsumer<NotificationMiddlewareContext> Middleware
        {
            get;
        }
        #endregion

        public ApplicationNotificationContext(INotificationQueue queue, IMiddlewareConsumer<NotificationMiddlewareContext> middleware)
        {
            Queue      = queue;
            Middleware = middleware;
        }
    }
    
    public interface IApplicationMessagingHost : IApplicationHost
    {
        #region Events
        event StructEventHandler<PeerJoinEventArgs> Join;
        event StructEventHandler<PeerResetEventArgs> Reset; 
        #endregion
        
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
    
    public sealed class ApplicationResources
    {
        #region Properties
        public IMessagePool Messages
        {
            get;
        }

        public IArrayPool<byte> Buffers
        {
            get;
        }

        public IPool<Request> Requests
        {
            get;
        }

        public IPool<Response> Responses
        {
            get;
        }
        #endregion

        public ApplicationResources(IMessagePool messages,
                                    IArrayPool<byte> buffers,
                                    IPool<Request> requests,
                                    IPool<Response> responses)
        {
            Messages  = messages ?? throw new ArgumentNullException(nameof(messages));
            Buffers   = buffers ?? throw new ArgumentNullException(nameof(buffers));
            Requests  = requests ?? throw new ArgumentNullException(nameof(requests));
            Responses = responses ?? throw new ArgumentNullException(nameof(responses));
        }
    }
    
    public abstract class Application : IApplication, IApplicationScriptingHost
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IRequestRouter requestRouter;
        private readonly INotificationCenter notificationCenter;
        private readonly IScriptManager scripts;
        
        private readonly IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private readonly IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        private readonly IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        
        private readonly ApplicationResources resources;
        private readonly Kernel kernel;

        private readonly IMessageSerializer serializer;
        private readonly IServer server;
        
        private readonly IApplicationTimer timer;
        
        private readonly Queue<ServerMessageEventArgs> outgoingEvents;
        private readonly Queue<PeerMessageEventArgs> incomingEvents;
        
        private readonly Queue<PeerResetEventArgs> resetsEvents;
        private readonly Queue<PeerJoinEventArgs> joinEvents;

        private bool running;
        #endregion
        
        #region Events
        public event EventHandler Starting;
        public event EventHandler ShuttingDown;
        
        public event StructEventHandler<PeerJoinEventArgs> Join;
        public event StructEventHandler<PeerResetEventArgs> Reset;
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

        public IScriptHost Scripts
            => scripts;

        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses => responseMiddleware;
        
        public IApplicationClock Clock => timer;
        #endregion

        public Application(Kernel kernel, 
                           ApplicationResources resources,
                           IRequestRouter requestRouter,
                           INotificationCenter notificationCenter,
                           IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware,
                           IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware,
                           IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware,
                           IServer server,
                           IScriptManager scripts,
                           IMessageSerializer serializer,
                           IApplicationTimer timer)
        {
            this.kernel     = kernel ?? throw new ArgumentNullException(nameof(kernel));
            this.resources  = resources ?? throw new ArgumentNullException(nameof(resources));
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.timer      = timer ?? throw new ArgumentNullException(nameof(timer));
            this.scripts    = scripts ?? throw new ArgumentNullException(nameof(scripts));

            this.requestRouter          = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
            this.notificationCenter    = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            Requests      = new ApplicationRequestContext(requestRouter, requestMiddleware);
            Notifications = new ApplicationNotificationContext(notificationCenter, notificationMiddleware);
            
            outgoingEvents = new Queue<ServerMessageEventArgs>();
            incomingEvents = new Queue<PeerMessageEventArgs>();
            resetsEvents   = new Queue<PeerResetEventArgs>();
            joinEvents     = new Queue<PeerJoinEventArgs>();
        }

        #region Event handlers
        private void Server_OnOutgoing(object sender, in ServerMessageEventArgs e)
        {
            resources.Buffers.Return(e.Data);
            
            outgoingEvents.Enqueue(e);
        }
            
        private void Server_OnIncoming(object sender, in PeerMessageEventArgs e)
            => incomingEvents.Enqueue(e);

        private void Server_OnReset(object sender, in PeerResetEventArgs e)
            => resetsEvents.Enqueue(e);

        private void Server_OnJoin(object sender, in PeerJoinEventArgs e)
            => joinEvents.Enqueue(e);
        #endregion
        
        private void ReleaseRequest(Request request)
        {
            if (request.Message != null)
                resources.Messages.Return(request.Message);
                    
            if (request.Contents != null)
                resources.Buffers.Return(request.Contents);
            
            resources.Requests.Return(request);
        }
            
        private void ReleaseResponse(Response response)
        {
            if (response.ContainsReply)
                resources.Messages.Return(response.Message);
                
            resources.Responses.Return(response);
        }
        
        private void Initialize()
        {
            Starting?.Invoke(this, EventArgs.Empty);
               
            server.Join     += Server_OnJoin;
            server.Reset    += Server_OnReset;
            server.Incoming += Server_OnIncoming;    
            server.Outgoing += Server_OnOutgoing;
            
            running = true;
        }

        private void Deinitialize()
        {
            ShuttingDown?.Invoke(this, EventArgs.Empty);
        }
        
        private void HandleServerEvents(HashSet<int> leavedPeers)
        {
            leavedPeers.Clear();
            
            while (joinEvents.Count != 0)
            {
                var joinEvent = joinEvents.Dequeue();
                
                Join?.Invoke(this, joinEvent);
            }
            
            while (resetsEvents.Count != 0)
            {
                var resetEvent = resetsEvents.Dequeue();
                
                Reset?.Invoke(this, resetEvent);
                
                leavedPeers.Add(resetEvent.Peer.Id);
            }
        }
        
        private void DeserializePeerMessages(HashSet<int> leavedPeers, Queue<Request> incomingRequests)
        {
            while (incomingEvents.Count != 0)
            {
                var incomingEvent = incomingEvents.Dequeue();
                
                if (leavedPeers.Contains(incomingEvent.Peer.Id))
                {
                    resources.Buffers.Return(incomingEvent.Data);
                    
                    continue;
                }
                
                var offset = 0;
                    
                while (offset < incomingEvent.Length)
                {
                    var request = resources.Requests.Take();

                    try
                    {
                        request.Message  = serializer.Deserialize(incomingEvent.Data, offset);
                        request.Contents = incomingEvent.Data;
                        request.Peer     = incomingEvent.Peer;
                    
                        incomingRequests.Enqueue(request);
                        
                        offset += serializer.GetSizeFromBuffer(incomingEvent.Data, offset);
                    }
                    catch (Exception e)
                    {
                        ReleaseRequest(request);
                    
                        Log.Warn(e, "exception occurred while processing request from peer");
                    }
                }
            }
        }
        
        private void RunPeerRequestMiddleware(Queue<Request> incomingRequests, Queue<Request> acceptedRequests)
        {
            while (incomingRequests.Count != 0)
            {
                var request = incomingRequests.Dequeue();
                
                try
                {   
                    if (requestMiddleware.Invoke(new RequestMiddlewareContext(request)))
                        ReleaseRequest(request);
                    else
                        acceptedRequests.Enqueue(request);
                }
                catch (Exception e)
                {
                    ReleaseRequest(request);
                    
                    Log.Warn(e, "excetion occurred in request middleware");
                }
            }
        }
        
        private void HandlePeerRequests(Queue<Request> acceptedRequests, Queue<RequestResponse> outgoingResponses)
        {
            while (acceptedRequests.Count != 0)
            {
                var request  = acceptedRequests.Dequeue();
                var response = resources.Responses.Take();

                try
                {
                    requestRouter.Dispatch(request, response);

                    switch (response.StatusCode)
                    {
                        case ResponseStatus.Code.Empty:
                            Log.Warn("received empty response from handler", request);
                            break;
                        case ResponseStatus.Code.Ok:
                            Log.Info("request was handled successfully", response, request);
                            break;
                        case ResponseStatus.Code.Reset:
                            Log.Info("request will reset the peer", response, request);
                            break;
                        case ResponseStatus.Code.ServerError:
                            Log.Warn("server error occurred whle processing request", response, request);
                            break;
                        case ResponseStatus.Code.BadRequest:
                            Log.Warn("handler received bad request", response, request);
                            break;
                        case ResponseStatus.Code.NoRoute:
                            Log.Warn("no route accepted the request", response, request);
                            break;
                        default:
                            throw new InvalidOrUnsupportedException(nameof(ResponseStatus.Code), response.StatusCode);
                    }

                    outgoingResponses.Enqueue(new RequestResponse(request, response));
                }
                catch (Exception e)
                {
                    ReleaseRequest(request);
                    ReleaseResponse(response);
                    
                    Log.Warn(e, "unhandled server error occurred while handling request");
                }
            }
        }
        
        private void UpdateServices()
        {
            foreach (var service in kernel.All<IActiveApplicationService>())
            {
                try
                {
                    service.Tick();
                }
                catch (Exception e)
                {
                    Log.Error(e, "exception occurred while updating service", service);
                }
            }
        }
        
        private void UpdateScripts()
            => scripts.Tick();
        
        private void RunPeerRequestResponseMiddleware()
        {
            throw new NotImplementedException();
        }
        
        private void SendRequestResponses()
        {
            throw new NotImplementedException();
        }

        private void RunNotificationMiddleware()
        {
            throw new NotImplementedException();
        }
        
        private void SendNotifications()
        {
            throw new NotImplementedException();
        }
        
        private void HandleServerMessages()
        {
            throw new NotImplementedException();
        }
        
        private void Tick(Queue<Request> incomingRequests, 
                          Queue<Request> acceptedRequests, 
                          Queue<RequestResponse> outgoingResponses, 
                          Queue<RequestResponse> acceptedResponses, 
                          Queue<Notification> outgoingNotifications, 
                          Queue<Notification> acceptedNotifications, 
                          HashSet<int> leavedPeers)
        {
            timer.Tick();
            
            HandleServerEvents(leavedPeers);
            
            DeserializePeerMessages(leavedPeers, incomingRequests);
            RunPeerRequestMiddleware(incomingRequests, acceptedRequests);
            HandlePeerRequests(acceptedRequests, outgoingResponses);
            
            UpdateServices();
            UpdateScripts();
            
            RunPeerRequestResponseMiddleware(outgoingResponses, acceptedResponses);
            HandleEnqueuedNotifications(outgoingNotifications, acceptedNotifications);
            
            SendRequestResponses(acceptedResponses);
            SendNotifications(acceptedNotifications);
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

            var incomingRequests = new Queue<Request>();
            var acceptedRequests = new Queue<Request>();
            
            var outgoingResponses = new Queue<RequestResponse>();
            var acceptedResponses = new Queue<RequestResponse>();
            
            var outgoingNotifications = new Queue<Notification>();
            var acceptedNotifications = new Queue<Notification>();

            var leavedPeers = new HashSet<int>();
            
            while (running)
                Tick(incomingRequests,
                     acceptedRequests,
                     outgoingResponses,
                     acceptedResponses,
                     outgoingNotifications,
                     acceptedNotifications,
                     leavedPeers);
            
            Deinitialize();
        }
    }
}