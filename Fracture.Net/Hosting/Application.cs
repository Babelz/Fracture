using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
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
    /// Flags for defining peer pipeline fidelity. Use these flags to enable immediate peer resets from application layer when unhandled error occurrs.
    /// </summary>
    [Flags]
    public enum PeerPipelineFidelity : byte
    {
        /// <summary>
        /// Least strict peer pipeline fidelity. All exceptions are omitted and peers will not be reset.
        /// </summary>
        Loose = 0,
        
        /// <summary>
        /// Peer will be reset if unhandled error occurs while peer is joining.
        /// </summary>
        Join = (1 << 0),

        /// <summary>
        /// Peer will be reset if unhandled error occurs while deserializing peer request.
        /// </summary>
        Receive = (1 << 1),
        
        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer request.
        /// </summary>
        Request = (1 << 2),
        
        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer notification.
        /// </summary>
        Notification = (1 << 3),
        
        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer response.
        /// </summary>
        Response = (1 << 4),
        
        /// <summary>
        /// Most strict mode. Any unhandled errors will cause the peer to reset immediately.
        /// </summary>
        Strict = Join | Receive | Request | Notification | Response
    }

    /// <summary>
    /// Class that provides functionality for creating net applications. Application provides request and response pipeline support for handling peer requests
    /// and state. 
    /// </summary>
    public sealed class Application
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
        
        private readonly PeerPipelineFidelity fidelity;
        
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
        public ApplicationRequestContext Requests
        {
            get;
        }

        public ApplicationNotificationContext Notifications
        {
            get;
        }
        
        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses
            => responseMiddleware;

        public IApplicationClock Clock 
            => timer;
        
        public IEnumerable<int> Peers
            => server.Peers;
        #endregion

        [BindingConstructor]
        public Application(IServer server,
                           IRequestRouter requestRouter,
                           INotificationCenter notificationCenter,
                           IMiddlewarePipeline<RequestMiddlewareContext> requestMiddleware,
                           IMiddlewarePipeline<NotificationMiddlewareContext> notificationMiddleware,
                           IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware,
                           IMessageSerializer serializer,
                           IApplicationTimer timer,
                           PeerPipelineFidelity fidelity)
        {
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.timer      = timer ?? throw new ArgumentNullException(nameof(timer));

            this.requestRouter          = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
            this.notificationCenter     = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));
            
            this.fidelity = fidelity;
            
            Requests      = new ApplicationRequestContext(requestRouter, requestMiddleware);
            Notifications = new ApplicationNotificationContext(notificationCenter, notificationMiddleware);
            
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
                Message.Release(notification.Message);
            
            Notification.Return(notification);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseRequest(Request request)
        {
            if (request.Message != null)
                Message.Release(request.Message);
                    
            if (request.Contents != null)
                BufferPool.Return(request.Contents);
            
            Request.Return(request);
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseResponse(Response response)
        {
            if (response.ContainsReply)
                Message.Release(response.Message);
                
            Response.Return(response);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReleaseRequestResponse(in RequestResponse requestResponse)
        {
            ReleaseRequest(requestResponse.Request);
            ReleaseResponse(requestResponse.Response);
        }
        
        private void HandlePeerFidelityViolation(PeerPipelineFidelity flag, int peerId)
        {
            if ((fidelity & flag) != flag) 
                return;
            
            Log.Warn($"marking peer {peerId} to be reset as it broke required fidelity level of " +
                     $"{Enum.GetName(typeof(PeerPipelineFidelity), flag)}");
                
            leavingPeers.Add(peerId);
        }

        #region Event handlers
        private static void Server_OnOutgoing(object sender, in ServerMessageEventArgs e)
            => BufferPool.Return(e.Contents);
            
        private void Server_OnIncoming(object sender, in PeerMessageEventArgs e)
            => incomingEvents.Enqueue(e);

        private void Server_OnReset(object sender, in PeerResetEventArgs e)
            => resetsEvents.Enqueue(e);

        private void Server_OnJoin(object sender, in PeerJoinEventArgs e)
            => joinEvents.Enqueue(e);
        #endregion

        private void Initialize()
        {
            // Hook events before any script does.
            server.Join     += Server_OnJoin;
            server.Reset    += Server_OnReset;
            server.Incoming += Server_OnIncoming;    
            server.Outgoing += Server_OnOutgoing;
   
            // Start the actual application by starting the server.
            Starting?.Invoke(this, EventArgs.Empty);

            server.Start();
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
                    Log.Info($"peer {joinEvent.Peer.Id} joining");

                    Join?.Invoke(this, joinEvent);
                }
                catch (Exception e)
                {
                    Log.Warn(e, "unhandled error occurred notifying about joining peer");
                    
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Join, joinEvent.Peer.Id);
                }
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
                    Log.Info($"peer {resetEvent.Peer.Id} resetting, reason: {resetEvent.Reason}");
                    
                    Reset?.Invoke(this, resetEvent);
                }
                catch (Exception e)
                {
                    Log.Warn(e, "unhandled error occurred notifying about reseting peer");
                }
                
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
                if (leavedPeers.Contains(incomingEvent.Peer.Id) || leavingPeers.Contains(incomingEvent.Peer.Id))
                {
                    BufferPool.Return(incomingEvent.Contents);
                    
                    continue;
                }
                
                var offset = 0;
                    
                // Deserialize all incoming messages in this event.
                while (offset < incomingEvent.Length)
                {
                    var request = Request.Take();

                    try
                    {
                        // Make sure next message in packet is non-zero sized.
                        var size = serializer.GetSizeFromBuffer(incomingEvent.Contents, offset);
                        
                        if (size == 0)
                        {
                            HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Peer.Id);
                            
                            Log.Warn($"packet contains zero sized message from peer {incomingEvent.Peer.Id}");
                            
                            BufferPool.Return(incomingEvent.Contents);
                            
                            ReleaseRequest(request);

                            break;
                        }
                        
                        if (size >= incomingEvent.Length - offset)
                        {
                            HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Peer.Id);
                            
                            Log.Warn($"invalid sized message received from peer {incomingEvent.Peer.Id}, reading further would go outside the bounds of the" +
                                     $" receive buffer");
                            
                            BufferPool.Return(incomingEvent.Contents);
                            
                            ReleaseRequest(request);

                            break;
                        }
                        
                        // Proceed to create request, get subset from received contents for single request.
                        var contents = BufferPool.Take(size);
                        
                        Array.Copy(incomingEvent.Contents, offset, contents, 0, size); 
                        
                        // Deserialize and create the actual request.
                        request.Message   = serializer.Deserialize(contents, 0);
                        request.Contents  = contents;
                        request.Peer      = incomingEvent.Peer;
                        request.Timestamp = incomingEvent.Timestamp;
                        
                        incomingRequests.Enqueue(request);

                        offset += size;
                    }
                    catch (Exception e)
                    {
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Peer.Id);
                        
                        ReleaseRequest(request);
                    
                        Log.Warn(e, "unhandled error occurred while processing request from peer");
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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Request, request.Peer.Id);
                    
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
                
                var response = Response.Take();

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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Request, request.Peer.Id);
                    
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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Response, requestResponse.Request.Peer.Id);
                    
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
                    foreach (var peer in peers)
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Notification, peer);

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
                    var contents = BufferPool.Take(size);
                    
                    serializer.Serialize(requestResponse.Response.Message, contents, 0);
                    
                    server.Send(requestResponse.Request.Peer.Id, contents, 0, size);
                }
                catch (Exception e)
                {
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Response, requestResponse.Request.Peer.Id);
                    
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
            void Send(IMessage message, int peer)
            {
                var size     = serializer.GetSizeFromMessage(message);
                var contents = BufferPool.Take(size);

                serializer.Serialize(message, contents, 0);
                    
                server.Send(peer, contents, 0, size);
            }
                
            void Broadcast(IMessage message, IEnumerable<int> peers)
            {
                var size     = serializer.GetSizeFromMessage(message);
                var contents = BufferPool.Take(size);

                serializer.Serialize(message, contents, 0);
                    
                foreach (var peer in peers)
                {
                    var copy = BufferPool.Take(size);
                        
                    contents.CopyTo(copy, 0);
                        
                    server.Send(peer, copy, 0, size);
                }
                
                BufferPool.Return(contents);
            }
            
            void Reset(IMessage message, IEnumerable<int> peers)
            {
                if (message != null)
                    Broadcast(message, peers);
                    
                foreach (var peer in peers)
                    leavingPeers.Add(peer);
            }
            
            while (acceptedNotifications.Count != 0)
            {
                var notification = acceptedNotifications.Dequeue();
                
                try
                {
                    switch (notification.Command)
                    {
                        case NotificationCommand.Send:
                            Send(notification.Message, notification.Peers.First());
                            break;
                        case NotificationCommand.BroadcastNarrow:
                            Broadcast(notification.Message, notification.Peers.Where(p => !leavingPeers.Contains(p)));
                            break;
                        case NotificationCommand.BroadcastWide:
                            Broadcast(notification.Message, server.Peers.Where(p => !leavingPeers.Contains(p)));
                            break;
                        case NotificationCommand.Reset:
                            Reset(notification.Message, notification.Peers.Where(p => !leavingPeers.Contains(p)));
                            break;
                        case NotificationCommand.Shutdown:
                            Reset(notification.Message, server.Peers.Where(p => !leavingPeers.Contains(p)));
                            break;
                        default:
                            Log.Error("notification command was unset or unsupported", notification);
                            break;
                    }
                }
                catch (Exception e)
                {
                    foreach (var peer in (notification.Peers ?? server.Peers).Where(p => !leavingPeers.Contains(p)))
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Notification, peer);
                    
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
            
            Log.Info("application shutdown signaled");
            
            running = false;
        }
        
        public void Start()
        {
            if (running)
                throw new InvalidOperationException("already running");
            
            Log.Info("starting application...");
            
            Initialize();

            running = true;
            
            Log.Info("entering application event loop");
            
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

            Log.Info("exited application event loop");
            
            Deinitialize();
        }
    }
}
