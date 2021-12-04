using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using NLog;

namespace Fracture.Net.Hosting
{
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
    /// Interface that provides common application host schematics between different host types.
    /// </summary>
    public interface IApplicationHost
    {
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
    /// Interface for application hosts that provides interface for services to interact with the application.
    /// </summary>
    public interface IApplicationServiceHost : IApplicationHost
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
    }
    
    /// <summary>
    /// Interface for application hosts that provides interface for scripts to interact with the application.
    /// </summary>
    public interface IApplicationScriptingHost : IApplicationHost
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
        ApplicationRequestContext Request
        {
            get;
        }

        /// <summary>
        /// Gets the application notification context for working with notification pipeline.
        /// </summary>
        ApplicationNotificationContext Notification
        {
            get;
        }
        
        /// <summary>
        /// Gets the application response middleware consumer.
        /// </summary>
        IMiddlewareConsumer<RequestResponseMiddlewareContext> Response
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Class for creating applications. Applications provide messaging pipeline and can be extended by services and scripts.
    /// </summary>
    public sealed class Application : IApplicationServiceHost, IApplicationScriptingHost
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IRequestRouter requestRouter;
        private readonly INotificationCenter notificationCenter;
        
        private readonly IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware;
        private readonly IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware;
        private readonly IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;
        
        private readonly IMessageSerializer serializer;
        private readonly IServer server;
        
        private readonly IApplicationTimer timer;
        
        private readonly Queue<PeerMessageEventArgs> incomingEvents;
        private readonly Queue<PeerResetEventArgs> resetsEvents;
        private readonly Queue<PeerJoinEventArgs> joinEvents;

        // Queue holding all incoming requests whose contents could we deserialized successfully.
        private readonly Queue<Request> incomingRequests;
        
        // Queue holding all incoming requests that were accepted by the middleware. 
        private readonly Queue<Request> acceptedRequests;
        
        // Queue holding all non-empty request objects generated by request handlers. 
        private readonly Queue<RequestResponse> outgoingResponses;
        
        // Queue holding all RR objects that were accepted by the middleware.
        private readonly Queue<RequestResponse> acceptedResponses;
        
        // Queue holding all notifications that were accepted by the middleware.
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
        
        public event EventHandler Tick;
        #endregion

        #region Properties
        public ApplicationRequestContext Request
        {
            get;
        }

        public ApplicationNotificationContext Notification
        {
            get;
        }
        
        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Response
            => responseMiddleware;
        
        public IApplicationClock Clock 
            => timer;
        #endregion

        public Application(IServer server,
                           IRequestRouter requestRouter,
                           INotificationCenter notificationCenter,
                           IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware,
                           IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware,
                           IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware,
                           IMessageSerializer serializer,
                           IApplicationTimer timer)
        {
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.timer      = timer ?? throw new ArgumentNullException(nameof(timer));

            this.requestRouter          = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
            this.notificationCenter     = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            Request      = new ApplicationRequestContext(requestRouter, requestMiddleware);
            Notification = new ApplicationNotificationContext(notificationCenter, notificationMiddleware);
            
            incomingEvents = new Queue<PeerMessageEventArgs>();
            resetsEvents   = new Queue<PeerResetEventArgs>();
            joinEvents     = new Queue<PeerJoinEventArgs>();
            
            incomingRequests = new Queue<Request>();
            acceptedRequests = new Queue<Request>();
            
            outgoingResponses     = new Queue<RequestResponse>();
            acceptedResponses     = new Queue<RequestResponse>();
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
        private static void Server_OnOutgoing(object sender, ServerMessageEventArgs e)
        {
            ServerResources.BlockBuffer.Return(e.Contents);
            
            ServerResources.EventArgs.ServerMessage.Return(e);
        }
            
        private void Server_OnIncoming(object sender, PeerMessageEventArgs e)
            => incomingEvents.Enqueue(e);

        private void Server_OnReset(object sender, PeerResetEventArgs e)
            => resetsEvents.Enqueue(e);

        private void Server_OnJoin(object sender, PeerJoinEventArgs e)
            => joinEvents.Enqueue(e);
        #endregion

        private void Initialize(int port, int backlog)
        {
            // Hook events before any script does.
            server.Join     += Server_OnJoin;
            server.Reset    += Server_OnReset;
            server.Incoming += Server_OnIncoming;    
            server.Outgoing += Server_OnOutgoing;
   
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
            {
                var joinEvent = joinEvents.Dequeue();
                
                try
                {
                    Join?.Invoke(this, joinEvent);
                }
                catch (Exception e)
                {
                    Log.Warn("unhandled error occurred notifying about joining peer");
                }
                
                ServerResources.EventArgs.PeerJoin.Return(joinEvent);
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
                
                try
                {
                    Reset?.Invoke(this, resetEvent);
                }
                catch (Exception e)
                {
                    Log.Warn("unhandled error occurred notifying about reseting peer");
                }
                
                ServerResources.EventArgs.PeerReset.Return(resetEvent);
                
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
                    ServerResources.EventArgs.PeerMessage.Return(incomingEvent);
                    
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
                    
                        Log.Warn(e, "unhandled error occurred while processing request from peer");
                    }
                }
                
                ServerResources.EventArgs.PeerMessage.Return(incomingEvent);
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
                    
                    Log.Warn(e, "unhandled error occurred in request middleware");
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
        /// Invoke tick event and notify external application consumers.
        /// </summary>
        private void TickApplication()
        {
            try
            {
                Tick?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                Log.Warn(e, "unhandled error occurred when invoking tick");
            }
        }
        
        /// <summary>
        /// Run response middleware for all outgoing responses.
        /// </summary>
        private void RunPeerRequestResponseMiddleware()
        {
            while (outgoingResponses.Count != 0)
            {
                var requestResponse = outgoingResponses.Dequeue();
                
                try
                {   
                    // Filter all requests that are rejected by the middleware.
                    if (responseMiddleware.Invoke(new RequestResponseMiddlewareContext(requestResponse.Request, requestResponse.Response)))
                        ReleaseRequestResponse(requestResponse);
                    else
                        acceptedResponses.Enqueue(requestResponse);
                }
                catch (Exception e)
                {
                    ReleaseRequestResponse(requestResponse);
                    
                    Log.Warn(e, "unhandled error occurred in request response middleware");
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
                        acceptedNotifications.Enqueue(notification);
                }
                catch (Exception e)
                {
                    ReleaseNotification(notification);
                    
                    Log.Warn(e, "unhandled error occurred in notification middleware");
                }
            });
        }
        
        /// <summary>
        /// Send out all enqueued responses. This can possibly disconnect peers.
        /// </summary>
        private void HandleAcceptedResponses()
        {
            while (acceptedResponses.Count != 0)
            {
                var requestResponse = acceptedResponses.Dequeue();

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
                    Log.Warn(e, "unhandled error occurred in notification middleware");
                }
                
                ReleaseRequestResponse(requestResponse);
            }
        }

        /// <summary>
        /// Send out all enqueued notifications.
        /// </summary>
        private void HandleAcceptedNotifications()
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
            
            while (acceptedNotifications.Count != 0)
            {
                var notification = acceptedNotifications.Dequeue();

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
                    Log.Warn(e, "unhandled error occurred in notification middleware");
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
                    Log.Error(e, "unhandled error occurred while disconnecting peer");
                }
            }
            
            leavingPeers.Clear();
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
            
                // Step 3: notify external application consumers about event cycle.
                TickApplication();
            
                // Step 4: run response middleware for responses generated at step 2. Run notification middleware for any notifications. 
                RunPeerRequestResponseMiddleware();
                RunNotificationMiddleware();       
            
                // Step 5: send out all requests and notifications generated by the server.
                HandleAcceptedResponses();         
                HandleAcceptedNotifications();     
            
                // Step 6: disconnect all leaving peers.
                ResetLeavingPeers(); 
            }

            Deinitialize();
        }
    }
}
