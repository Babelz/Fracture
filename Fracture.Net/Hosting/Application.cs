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
    /// <summary>
    /// Interface for implementing application hosts. Hosts provide interface for tracking application status and time. 
    /// </summary>
    public interface IApplicationHost
    {
        #region Events
        /// <summary>
        /// Event invoked when the application is about to start.
        /// </summary>
        event EventHandler Starting;
        
        /// <summary>
        /// Event invoked when the application is about to shut down.
        /// </summary>
        event EventHandler ShuttingDown;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the application clock containing current application time.
        /// </summary>
        IApplicationClock Clock
        {
            get;
        }
        #endregion

        /// <summary>
        /// Signals the application to start shutting down.
        /// </summary>
        void Shutdown();
    }
    
    /// <summary>
    /// Wrapper class to make working with requests bit more convenient. Use the <see cref="Router"/> to access the application request router and the
    /// <see cref="Middleware"/> to access the notification middleware.
    /// </summary>
    public sealed class ApplicationRequestContext
    {
        #region Properties
        public IRequestRouteConsumer Router
        {
            get;
        }
        
        public IMiddlewareConsumer<RequestMiddlewareContext> Middleware
        {
            get;
        }
        #endregion
        
        public ApplicationRequestContext(IRequestRouteConsumer router, IMiddlewareConsumer<RequestMiddlewareContext> middleware)
        {
            Router     = router;
            Middleware = middleware;
        }
    }
    
    /// <summary>
    /// Wrapper class to make working with notifications bit more convenient. Use the <see cref="Queue"/> to access the notification queue of the application
    /// and the <see cref="Middleware"/> to access the notification middleware.
    /// </summary>
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
    
    /// <summary>
    /// Interface for application hosts that provide messaging support.
    /// </summary>
    public interface IApplicationMessagingHost : IApplicationHost
    {
        #region Events
        /// <summary>
        /// Event invoked when peer has joined.
        /// </summary>
        event EventHandler<PeerJoinEventArgs> Join;
        
        /// <summary>
        /// Event invoked when peer has reset.
        /// </summary>
        event EventHandler<PeerResetEventArgs> Reset; 
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the application request context for working with the request pipeline.
        /// </summary>
        ApplicationRequestContext Requests
        {
            get;
        }

        /// <summary>
        /// Gets the application notification context for working with notification pipeline.
        /// </summary>
        ApplicationNotificationContext Notifications
        {
            get;
        }
        
        /// <summary>
        /// Gets the application response middleware consumer.
        /// </summary>
        IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses
        {
            get;
        }
        #endregion
    }
    
    /// <summary>
    /// Interface for application hosts that provide scripting support.
    /// </summary>
    public interface IApplicationScriptingHost : IApplicationMessagingHost
    {
        #region Properties
        /// <summary>
        /// Gets the application scripting host for working with scripts.
        /// </summary>
        public IScriptHost Scripts
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Interface for implementing applications. Applications provide messaging and peer status handling for all scripts and services.
    /// </summary>
    public interface IApplication : IApplicationHost
    {
        /// <summary>
        /// Starts the application and initializes all of its dependencies.
        /// </summary>
        void Start();
    }
    
    /// <summary>
    /// Wrapper class containing all application required resources.
    /// </summary>
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
        
        public IPool<Notification> Notifications
        {
            get;
        }
        #endregion

        public ApplicationResources(IMessagePool messages,
                                    IArrayPool<byte> buffers,
                                    IPool<Request> requests,
                                    IPool<Response> responses,
                                    IPool<Notification> notifications)
        {
            Messages      = messages ?? throw new ArgumentNullException(nameof(messages));
            Buffers       = buffers ?? throw new ArgumentNullException(nameof(buffers));
            Requests      = requests ?? throw new ArgumentNullException(nameof(requests));
            Responses     = responses ?? throw new ArgumentNullException(nameof(responses));
            Notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
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

        private readonly Queue<Request> incomingRequests;
        private readonly Queue<Request> acceptedRequests;
        
        private readonly Queue<RequestResponse> outgoingResponses;
        private readonly Queue<RequestResponse> acceptedResponses;
        
        private readonly Queue<Notification> outgoingNotifications;
        private readonly Queue<Notification> acceptedNotifications;
        
        private readonly HashSet<int> leavedPeers;
        private readonly HashSet<int> leavingPeers;
        
        private bool running;
        #endregion
        
        #region Events
        public event EventHandler Starting;
        public event EventHandler ShuttingDown;
        
        public event EventHandler<PeerJoinEventArgs> Join;
        public event EventHandler<PeerResetEventArgs> Reset;
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
            this.notificationCenter     = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            Requests      = new ApplicationRequestContext(requestRouter, requestMiddleware);
            Notifications = new ApplicationNotificationContext(notificationCenter, notificationMiddleware);
            
            outgoingEvents = new Queue<ServerMessageEventArgs>();
            incomingEvents = new Queue<PeerMessageEventArgs>();
            resetsEvents   = new Queue<PeerResetEventArgs>();
            joinEvents     = new Queue<PeerJoinEventArgs>();
            
            incomingRequests = new Queue<Request>();
            acceptedRequests = new Queue<Request>();
            
            outgoingResponses = new Queue<RequestResponse>();
            acceptedResponses = new Queue<RequestResponse>();
            
            outgoingNotifications = new Queue<Notification>();
            acceptedNotifications = new Queue<Notification>();

            leavedPeers  = new HashSet<int>();
            leavingPeers = new HashSet<int>();
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
        
        private void ReleaseNotification(Notification notification)
        {
            if (notification.Message != null)
                resources.Messages.Return(notification.Message);
            
            resources.Notifications.Return(notification);
        }
        
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
        
        private void ReleaseRequestResponse(in RequestResponse requestResponse)
        {
            ReleaseRequest(requestResponse.Request);
            ReleaseResponse(requestResponse.Response);
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
        
        /// <summary>
        /// Polls the server for incoming events and updates application time.
        /// </summary>
        private void PollApplication()
        {
            timer.Tick();
            
            server.Poll();
        }
        
        /// <summary>
        /// Handles received peer join events.
        /// </summary>
        private void HandlePeerJoins()
        {
            // Handle all join events.
            while (joinEvents.Count != 0)
            {
                var joinEvent = joinEvents.Dequeue();
                
                Join?.Invoke(this, joinEvent);
            }
        }

        /// <summary>
        /// Handles all reset peer events.
        /// </summary>
        private void HandlePeerLeaves()
        {
            // Make sure this poll frames data is clean.
            leavedPeers.Clear();
            
            // Handle all reset events.
            while (resetsEvents.Count != 0)
            {
                var resetEvent = resetsEvents.Dequeue();
                
                Reset?.Invoke(this, resetEvent);
                
                leavedPeers.Add(resetEvent.Peer.Id);
            }
        }
        
        /// <summary>
        /// Deserialize incoming messages from peers.
        /// </summary>
        private void DeserializePeerMessages()
        {
            while (incomingEvents.Count != 0)
            {
                var incomingEvent = incomingEvents.Dequeue();
                
                // Skip handling for all peers that are about to leave.
                if (!server.Connected(incomingEvent.Peer.Id) || leavedPeers.Contains(incomingEvent.Peer.Id))
                {
                    resources.Buffers.Return(incomingEvent.Data);
                    
                    continue;
                }
                
                var offset = 0;
                    
                // Deserialize all incoming messages in this event.
                while (offset < incomingEvent.Length)
                {
                    var request = resources.Requests.Take();

                    try
                    {
                        request.Message   = serializer.Deserialize(incomingEvent.Data, offset);
                        request.Contents  = incomingEvent.Data;
                        request.Peer      = incomingEvent.Peer;
                        request.Timestamp = incomingEvent.Timestamp;
                    
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
        
        /// <summary>
        /// Runs middleware for all deserialized requests received from peers.
        /// </summary>
        private void RunPeerRequestMiddleware()
        {
            while (incomingRequests.Count != 0)
            {
                var request = incomingRequests.Dequeue();
                
                try
                {   
                    // Filter all requests that are rejected by the middleware.
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
        
        /// <summary>
        /// Handle all accepted peer requests.
        /// </summary>
        private void HandlePeerRequests()
        {
            while (acceptedRequests.Count != 0)
            {
                var request = acceptedRequests.Dequeue();
                
                // Do not allow handling of requests where the peer is marked for reset.
                if (leavingPeers.Contains(request.Peer.Id))
                {
                    ReleaseRequest(request);
                    
                    continue;
                }
                
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
                            
                            // Disallow sending any further messages if the peer is about to be reset.
                            leavingPeers.Add(request.Peer.Id);
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
        
        /// <summary>
        /// Runs update logic for all services attached to the application.
        /// </summary>
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
        
        /// <summary>
        /// Runs update logic for all scripts active inside the application.
        /// </summary>
        private void UpdateScripts()
            => scripts.Tick();

        /// <summary>
        /// Run response middleware for all outgoing responses.
        /// </summary>
        private void RunPeerRequestResponseMiddleware()
        {
            while (acceptedResponses.Count != 0)
            {
                var requestResponse = acceptedResponses.Dequeue();
                
                // Filter out all peers that are about to leave.
                if (leavingPeers.Contains(requestResponse.Request.Peer.Id))
                {
                    ReleaseRequestResponse(requestResponse);
                    
                    continue;
                }
                
                try
                {   
                    // Filter all requests that are rejected by the middleware.
                    if (responseMiddleware.Invoke(new RequestResponseMiddlewareContext(requestResponse.Request, requestResponse.Response)))
                        ReleaseRequestResponse(requestResponse);
                    else
                        outgoingResponses.Enqueue(requestResponse);
                }
                catch (Exception e)
                {
                    ReleaseRequestResponse(requestResponse);
                    
                    Log.Warn(e, "excetion occurred in request response middleware");
                }
            }
        }
        
        /// <summary>
        /// Run notification middleware for all enqueued notifications.
        /// </summary>
        private void RunNotificationMiddleware()
        {
            notificationCenter.Handle((notification) =>
            {
                // Filter out all peers that are about to leave. 
                var peers = (notification.Peers ?? server.Peers).Where(p => !leavedPeers.Contains(p)).ToArray();
                
                try
                {   
                    // Filter all requests that are rejected by the middleware.
                    if (notificationMiddleware.Invoke(new NotificationMiddlewareContext(peers, notification)))
                        ReleaseNotification(notification);
                    else
                        outgoingNotifications.Enqueue(notification);
                }
                catch (Exception e)
                {
                    ReleaseNotification(notification);
                    
                    Log.Warn(e, "excetion occurred in notification middleware");
                }
            });
        }
        
        private void SendRequestResponses()
        {
            while (outgoingResponses.Count != 0)
            {
                
            }
        }

        private void SendNotifications()
        {
            throw new NotImplementedException();
        }
        
        private void ResetLeavingPeers()
        {
            
        }

        /// <summary>
        /// Runs the application logic once. This includes running the request, response and notification pipelines where scripts and services are allowed to
        /// interact with the application by being updated in middle.
        /// </summary>
        private void Tick()
        {
            // Step 1: poll server and receive events, update application time. Handle all incoming peer leave and join events first and track all peers that
            //         are about to leave.
            PollApplication();
            
            HandlePeerJoins();
            HandlePeerLeaves();
            
            // Step 2: run the actual request pipeline. Deserialize all incoming messages, run request middleware and route the requests.  
            DeserializePeerMessages();
            RunPeerRequestMiddleware();
            HandlePeerRequests();
            
            // Step 3: allow services and scripts to update their status. At this point scripts can also send out any notifications.
            UpdateServices();
            UpdateScripts();
            
            // Step 4: run response middleware for responses generated at step 2. Run notification middleware for any notifications. 
            RunPeerRequestResponseMiddleware();
            RunNotificationMiddleware();
            
            // Step 5: send out all requests and notifications generated by the server.
            SendRequestResponses();
            SendNotifications();
            
            // Step 6: disconnect all leaving peers.
            ResetLeavingPeers();
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