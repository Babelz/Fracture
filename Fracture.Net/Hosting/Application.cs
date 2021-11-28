using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Memory.Pools;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
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
        IApplicationScriptLoader Scripts
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
        void Start(int port, int backlog);
    }
    
    public sealed class Application : IApplication, IApplicationScriptingHost
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IRequestRouter requestRouter;
        private readonly INotificationCenter notificationCenter;
        
        private readonly IApplicationScriptManager scripts;
        private readonly IApplicationServiceManager services;
        
        private readonly IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private readonly IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        private readonly IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        
        private readonly Kernel kernel;

        private readonly IMessageSerializer serializer;
        private readonly IServer server;
        
        private readonly IApplicationTimer timer;
        
        private readonly Queue<PeerMessageEventArgs> incomingEvents;
        private readonly Queue<PeerResetEventArgs> resetsEvents;
        private readonly Queue<PeerJoinEventArgs> joinEvents;

        private readonly Queue<Request> incomingRequests;
        private readonly Queue<Request> acceptedRequests;
        
        private readonly Queue<RequestResponse> outgoingResponses;
        private readonly Queue<RequestResponse> acceptedResponses;
        
        private readonly Queue<Notification> outgoingNotifications;
        private readonly Queue<Notification> acceptedNotifications;
        
        // Peers that were marked as leaving by the server during poll call.
        private readonly HashSet<int> leavedPeers;
        
        // Peers that were marked as leaving by message pipeline.
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
        
        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses => responseMiddleware;
        
        public IApplicationClock Clock => timer;
        
        public IApplicationScriptLoader Scripts
            => scripts;
        #endregion

        public Application(Kernel kernel, 
                           IRequestRouter requestRouter,
                           INotificationCenter notificationCenter,
                           IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware,
                           IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware,
                           IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware,
                           IServer server,
                           IApplicationScriptManager scripts,
                           IApplicationServiceManager services,
                           IMessageSerializer serializer,
                           IApplicationTimer timer)
        {
            this.kernel     = kernel ?? throw new ArgumentNullException(nameof(kernel));
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.timer      = timer ?? throw new ArgumentNullException(nameof(timer));
            this.scripts    = scripts ?? throw new ArgumentNullException(nameof(scripts));
            this.services   = services ?? throw new ArgumentNullException(nameof(services));

            this.requestRouter          = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
            this.notificationCenter     = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            Requests      = new ApplicationRequestContext(requestRouter, requestMiddleware);
            Notifications = new ApplicationNotificationContext(notificationCenter, notificationMiddleware);
            
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseNotification(Notification notification)
        {
            if (notification.Message != null)
                Message.Return(notification.Message);
            
            ApplicationResources.Notification.Return(notification);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseRequest(Request request)
        {
            if (request.Message != null)
                Message.Return(request.Message);
                    
            if (request.Contents != null)
                ServerResources.BlockBuffer.Return(request.Contents);
            
            ApplicationResources.Request.Return(request);
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseResponse(Response response)
        {
            if (response.ContainsReply)
                Message.Return(response.Message);
                
            ApplicationResources.Response.Return(response);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseRequestResponse(in RequestResponse requestResponse)
        {
            ReleaseRequest(requestResponse.Request);
            ReleaseResponse(requestResponse.Response);
        }
        
        #region Event handlers
        private static void Server_OnOutgoing(object sender, in ServerMessageEventArgs e)
            => ServerResources.BlockBuffer.Return(e.Contents);
           
        private void Server_OnIncoming(object sender, in PeerMessageEventArgs e)
            => incomingEvents.Enqueue(e);

        private void Server_OnReset(object sender, in PeerResetEventArgs e)
            => resetsEvents.Enqueue(e);

        private void Server_OnJoin(object sender, in PeerJoinEventArgs e)
            => joinEvents.Enqueue(e);
        #endregion

        private void Initialize(int port, int backlog)
        {
            // Hook events before any script does.
            server.Join     += Server_OnJoin;
            server.Reset    += Server_OnReset;
            server.Incoming += Server_OnIncoming;    
            server.Outgoing += Server_OnOutgoing;

            // Initialize scripts and services before startup to allow them run possible initialization logic before starting.
            services.Initialize(kernel);
            
            scripts.Initialize(kernel);
            
            // Start the actual application by starting the server.
            Starting?.Invoke(this, EventArgs.Empty);

            server.Start(port, backlog);
        }

        private void Deinitialize()
        {
            ShuttingDown?.Invoke(this, EventArgs.Empty);
            
            server.Shutdown();
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
                Join?.Invoke(this, joinEvents.Dequeue());
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
        /// Deserialize incoming messages from peers. All requests from peers that have been marked as leaved by server will be filtered
        /// out and not handled at all.
        /// </summary>
        private void DeserializePeerMessages()
        {
            while (incomingEvents.Count != 0)
            {
                var incomingEvent = incomingEvents.Dequeue();
                
                // Skip handling for all requests where the peer has leaved.
                if (leavedPeers.Contains(incomingEvent.Peer.Id))
                {
                    ServerResources.BlockBuffer.Return(incomingEvent.Contents);
                    
                    continue;
                }
                
                var offset = 0;
                    
                // Deserialize all incoming messages in this event.
                while (offset < incomingEvent.Length)
                {
                    var request = ApplicationResources.Request.Take();

                    try
                    {
                        request.Message   = serializer.Deserialize(incomingEvent.Contents, offset);
                        request.Contents  = incomingEvent.Contents;
                        request.Peer      = incomingEvent.Peer;
                        request.Timestamp = incomingEvent.Timestamp;
                    
                        incomingRequests.Enqueue(request);
                        
                        offset += serializer.GetSizeFromBuffer(incomingEvent.Contents, offset);
                    }
                    catch (Exception e)
                    {
                        ReleaseRequest(request);
                    
                        Log.Warn(e, "error occurred while processing request from peer");
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
        /// Handle all accepted peer requests. If the handler resets the peer any remaining requests will not be handled.
        /// </summary>
        private void HandlePeerRequests()
        {
            while (acceptedRequests.Count != 0)
            {
                var request = acceptedRequests.Dequeue();
                
                // Do not allow handling of requests if the handler reset the peer.
                if (leavingPeers.Contains(request.Peer.Id))
                {
                    ReleaseRequest(request);
                    
                    continue;
                }
                
                var response = ApplicationResources.Response.Take();

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
                            
                            // Disallow handling of any remaining requests and mark the peer as leaving.
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
                    
                    if (response.ContainsException)
                        Log.Warn(response.Exception, "response object contains expection");
                    
                    if (response.ContainsReply)
                        outgoingResponses.Enqueue(new RequestResponse(request, response));
                    else
                    {
                        ReleaseRequest(request);
                        ReleaseResponse(response);
                    }
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
            => services.Tick();
        
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
            // Notification ownership is moved to the application at this point.
            notificationCenter.Handle((notification) =>
            {
                // All notifications might not have peers associated with them. In this case we broadcast the notification to all peers.
                var peers = (notification.Peers ?? server.Peers).ToArray();

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
        
        /// <summary>
        /// Send out all enqueued responses. This can possibly disconnect peers.
        /// </summary>
        private void HandleOutgoingResponses()
        {
            while (outgoingResponses.Count != 0)
            {
                var requestResponse = outgoingResponses.Dequeue();

                try
                {
                    // If an exception is thrown here we will leak the buffer and GC will be able to collect it.
                    var size     = serializer.GetSizeFromMessage(requestResponse.Response.Message);
                    var contents = ServerResources.BlockBuffer.Take(size);
                    
                    serializer.Serialize(requestResponse.Response.Message, contents, 0);
                    
                    server.Send(requestResponse.Request.Peer.Id, contents, 0, contents.Length);
                }
                catch (Exception e)
                {
                    Log.Warn(e, "exception occurred in notification middleware");
                }
                
                ReleaseRequestResponse(requestResponse);
            }
        }

        /// <summary>
        /// Send out all enqueued responses
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void HandleOutgoingNotifications()
        {
            void Send(INotification notification)
            {
                var peer = notification.Peers.First();
                
                if (leavingPeers.Contains(peer))
                    return;
                
                var contents = ServerResources.BlockBuffer.Take(serializer.GetSizeFromMessage(notification.Message));

                serializer.Serialize(notification.Message, contents, 0);
                    
                server.Send(peer, contents, 0, contents.Length);
            }
                
            void BroadcastNarrow(INotification notification)
            {
                var contents = ServerResources.BlockBuffer.Take(serializer.GetSizeFromMessage(notification.Message));

                serializer.Serialize(notification.Message, contents, 0);
                    
                foreach (var peer in notification.Peers.Where(p => !leavingPeers.Contains(p)))
                {
                    var copy = ServerResources.BlockBuffer.Take(contents.Length);
                        
                    contents.CopyTo(copy, 0);
                        
                    server.Send(peer, copy, 0, copy.Length);
                }
            }
                
            void BroadcastWide(INotification notification)
            {
                var contents = ServerResources.BlockBuffer.Take(serializer.GetSizeFromMessage(notification.Message));

                serializer.Serialize(notification.Message, contents, 0);
                    
                foreach (var peer in server.Peers.Where(p => !leavingPeers.Contains(p)))
                {
                    var copy = ServerResources.BlockBuffer.Take(contents.Length);
                        
                    contents.CopyTo(copy, 0);
                        
                    server.Send(peer, copy, 0, copy.Length);
                }
            }
                
            void Reset(INotification notification)
            {
                byte[] contents = null;
                
                if (notification.Message != null)
                {
                    var size = serializer.GetSizeFromMessage(notification.Message);
                    
                    contents = ServerResources.BlockBuffer.Take(size);
                    
                    serializer.Serialize(notification.Message, contents, 0);
                }
                
                foreach (var peer in (notification.Peers ?? server.Peers).Where(p => !leavingPeers.Contains(p)))
                {
                    if (contents != null)
                    {
                        var copy = ServerResources.BlockBuffer.Take(contents.Length);
                        
                        contents.CopyTo(copy, 0);
                        
                        server.Send(peer, copy, 0, copy.Length);
                    }
                    
                    leavingPeers.Add(peer);
                }
            }
            
            while (outgoingResponses.Count != 0)
            {
                var notification = outgoingNotifications.Dequeue();

                try
                {
                    switch (notification.Command)
                    {
                        case NotificationCommand.Send:
                            Send(notification);
                            break;
                        case NotificationCommand.BroadcastNarrow:
                            BroadcastNarrow(notification);
                            break;
                        case NotificationCommand.BroadcastWide:
                            BroadcastWide(notification);
                            break;
                        case NotificationCommand.Reset:
                            Reset(notification);
                            break;
                        default:
                            Log.Error("notification command was unset or unsupported", notification);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Warn(e, "excetion occurred in notification middleware");
                }
                
                ReleaseNotification(notification);
            }
        }
        
        /// <summary>
        /// Resets (disconnects) all peers that have been marked as leaving.
        /// </summary>
        private void ResetLeavingPeers()
        {
            foreach (var peer in leavingPeers)
            {
                try
                {
                    server.Disconnect(peer);
                }
                catch (Exception e)
                {
                    Log.Error(e, "error occurred while disconnecting peer");
                }
            }
            
            leavingPeers.Clear();
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
            HandleOutgoingResponses();
            HandleOutgoingNotifications();
            
            // Step 6: disconnect all leaving peers.
            ResetLeavingPeers();
        }

        public void Shutdown()
        {
            if (!running)
                throw new InvalidOperationException("already running");
            
            running = false;
        }

        public void Start(int port, int backlog)
        {
            if (running)
                throw new InvalidOperationException("already running");
            
            Initialize(port, backlog);

            running = true;
            
            while (running)
                Tick();

            Deinitialize();
        }
    }
}
