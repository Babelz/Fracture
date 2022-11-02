using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Common.Runtime;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Fracture.Net.Messages;
using Serilog;

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
    /// Flags for defining peer pipeline fidelity. Use these flags to enable immediate peer resets from application layer when unhandled error occurs.
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
        Join = 1 << 0,

        /// <summary>
        /// Peer will be reset if unhandled error occurs while deserializing peer request.
        /// </summary>
        Receive = 1 << 1,

        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer request.
        /// </summary>
        Request = 1 << 2,

        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer notification.
        /// </summary>
        Notification = 1 << 3,

        /// <summary>
        /// Peer will be reset if unhandled error occurs while handling peer response.
        /// </summary>
        Response = 1 << 4,

        /// <summary>
        /// Most strict mode. Any unhandled errors will cause the peer to reset immediately.
        /// </summary>
        Strict = Join | Receive | Request | Notification | Response,
    }

    /// <summary>
    /// Flags for defining how any exceptions in the pipeline should be handled. This is useful for debugging and test purposes.
    /// </summary>
    public enum PipelineExceptionFidelity : byte
    {
        /// <summary>
        /// All exceptions in the pipeline are ingested and reported back via logging. 
        /// </summary>
        Ingest = 0,

        /// <summary>
        /// All exceptions in the pipeline are re-thrown.
        /// </summary>
        Throw,
    }

    /// <summary>
    /// Class that provides functionality for creating net applications. Application provides request and response pipeline support for handling peer requests
    /// and state. 
    /// </summary>
    public sealed class Application
    {
        #region Fields
        private readonly IRequestRouter      requestRouter;
        private readonly INotificationCenter notificationCenter;

        private readonly IMiddlewarePipeline<RequestMiddlewareContext>         requestMiddleware;
        private readonly IMiddlewarePipeline<NotificationMiddlewareContext>    notificationMiddleware;
        private readonly IMiddlewarePipeline<RequestResponseMiddlewareContext> responseMiddleware;

        private readonly IMessageSerializer serializer;

        private readonly IServer server;

        private readonly IApplicationTimer timer;

        private readonly PeerPipelineFidelity      peerFidelity;
        private readonly PipelineExceptionFidelity exceptionFidelity;

        private readonly Queue<PeerMessageEventArgs> incomingEvents;
        private readonly Queue<PeerResetEventArgs>   resetsEvents;
        private readonly Queue<PeerJoinEventArgs>    joinEvents;

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
        private readonly HashSet<int> leavingPeerIds;

        private ExecutionTimer applicationLoopTimer;

        private bool running;
        #endregion

        #region Events
        public event EventHandler Starting;

        public event EventHandler ShuttingDown;

        public event StructEventHandler<PeerJoinEventArgs> Join;

        public event StructEventHandler<PeerResetEventArgs> Reset;

        public event StructEventHandler<PeerMessageEventArgs> BadRequest;

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

        public IMiddlewareConsumer<RequestResponseMiddlewareContext> Responses => responseMiddleware;

        public IApplicationClock Clock => timer;

        public IEnumerable<int> PeerIds => server.PeerIds;
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
                           PeerPipelineFidelity peerFidelity,
                           PipelineExceptionFidelity exceptionFidelity)
        {
            this.server     = server ?? throw new ArgumentNullException(nameof(server));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.timer      = timer ?? throw new ArgumentNullException(nameof(timer));

            this.requestRouter          = requestRouter ?? throw new ArgumentNullException(nameof(requestRouter));
            this.notificationCenter     = notificationCenter ?? throw new ArgumentNullException(nameof(notificationCenter));
            this.requestMiddleware      = requestMiddleware ?? throw new ArgumentNullException(nameof(requestMiddleware));
            this.notificationMiddleware = notificationMiddleware ?? throw new ArgumentNullException(nameof(notificationMiddleware));
            this.responseMiddleware     = responseMiddleware ?? throw new ArgumentNullException(nameof(responseMiddleware));

            this.peerFidelity      = peerFidelity;
            this.exceptionFidelity = exceptionFidelity;

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

            leavedPeers    = new HashSet<int>();
            leavingPeerIds = new HashSet<int>();

            applicationLoopTimer = new ExecutionTimer("application-loop-round", TimeSpan.FromSeconds(15));
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

        private void HandlePeerFidelityViolation(PeerPipelineFidelity flag, int peer)
        {
            if ((peerFidelity & flag) != flag)
                return;

            Log.Warning($"marking peer {peer} to be reset as it broke required fidelity level of " +
                        $"{Enum.GetName(typeof(PeerPipelineFidelity), flag)}");

            leavingPeerIds.Add(peer);
        }

        private void HandlePipelineExceptionFidelity(Exception exception)
        {
            if (exceptionFidelity == PipelineExceptionFidelity.Ingest)
                return;

            throw new Exception("unhandled pipeline exception occurred, pipeline is not configured to ingest exceptions", exception);
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

            notificationCenter.Shutdown();

            server.Shutdown();
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
                    Log.Information($"peer {joinEvent.Connection.PeerId} joining");

                    Join?.Invoke(this, joinEvent);
                }
                catch (Exception e)
                {
                    Log.Warning(e, "unhandled error occurred notifying about joining peer");

                    HandlePeerFidelityViolation(PeerPipelineFidelity.Join, joinEvent.Connection.PeerId);

                    HandlePipelineExceptionFidelity(e);
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
                    Log.Information($"peer {resetEvent.Connection.PeerId} resetting, reason: {resetEvent.Reason}");

                    Reset?.Invoke(this, resetEvent);
                }
                catch (Exception e)
                {
                    Log.Warning(e, "unhandled error occurred notifying about resetting peer");

                    HandlePipelineExceptionFidelity(e);
                }

                leavedPeers.Add(resetEvent.Connection.PeerId);
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

                // Skip handling for all requests where the peer has leaved or is about to leave.
                if (leavedPeers.Contains(incomingEvent.Connection.PeerId) || leavingPeerIds.Contains(incomingEvent.Connection.PeerId))
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
                            HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Connection.PeerId);

                            Log.Warning($"packet contains zero sized message from peer {incomingEvent.Connection.PeerId}");

                            BadRequest?.Invoke(this, incomingEvent);

                            ReleaseRequest(request);

                            break;
                        }

                        if (size > incomingEvent.Length - offset)
                        {
                            HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Connection.PeerId);

                            Log.Warning(
                                $"invalid sized message received from peer {incomingEvent.Connection.PeerId}, reading further would go outside the bounds of the" +
                                $" receive buffer");

                            BadRequest?.Invoke(this, incomingEvent);

                            ReleaseRequest(request);

                            break;
                        }

                        // Proceed to create request, get subset from received contents for single request.
                        var contents = BufferPool.Take(size);

                        Array.Copy(incomingEvent.Contents, offset, contents, 0, size);

                        // Deserialize and create the actual request.
                        request.Message    = serializer.Deserialize(contents, 0);
                        request.Contents   = contents;
                        request.Connection = incomingEvent.Connection;
                        request.Timestamp  = incomingEvent.Timestamp;
                        request.Length     = size;

                        incomingRequests.Enqueue(request);

                        offset += size;
                    }
                    catch (Exception e)
                    {
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Receive, incomingEvent.Connection.PeerId);

                        HandlePipelineExceptionFidelity(e);

                        ReleaseRequest(request);

                        BadRequest?.Invoke(this, incomingEvent);

                        Log.Warning(e, "unhandled error occurred while processing request from peer");

                        break;
                    }
                }

                BufferPool.Return(incomingEvent.Contents);
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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Request, request.Connection.PeerId);

                    HandlePipelineExceptionFidelity(e);

                    ReleaseRequest(request);

                    Log.Warning(e, "unhandled error occurred in request middleware");
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
                if (leavingPeerIds.Contains(request.Connection.PeerId))
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
                            Log.Warning("received empty response from handler, {@request}", request);

                            break;
                        case ResponseStatus.Code.Ok:
                            Log.Debug("request was handled successfully, {@response}, {@request}", response, request);

                            break;
                        case ResponseStatus.Code.Reset:
                            Log.Debug("request will reset the peer, {@response}, {@request}", response, request);

                            // Disallow handling of any remaining requests and mark the peer as leaving.
                            leavingPeerIds.Add(request.Connection.PeerId);

                            break;
                        case ResponseStatus.Code.ServerError:
                            Log.Debug("server error occurred while processing request, {@response}, {@request}", response, request);

                            break;
                        case ResponseStatus.Code.BadRequest:
                            Log.Debug("handler received bad request, {@response}, {@request}", response, request);

                            break;
                        case ResponseStatus.Code.NoRoute:
                            Log.Debug("no route accepted the request, {@response}, {@request}", response, request);

                            break;
                        default:
                            throw new InvalidOrUnsupportedException(nameof(ResponseStatus.Code), response.StatusCode);
                    }

                    if (response.ContainsException)
                        Log.Warning(response.Exception, "response object contains exception");

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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Request, request.Connection.PeerId);

                    HandlePipelineExceptionFidelity(e);

                    ReleaseRequest(request);
                    ReleaseResponse(response);

                    Log.Warning(e, "unhandled server error occurred while handling request");
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
                Log.Warning(e, "unhandled error occurred when invoking tick");

                HandlePipelineExceptionFidelity(e);
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
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Response, requestResponse.Request.Connection.PeerId);

                    HandlePipelineExceptionFidelity(e);

                    ReleaseRequestResponse(requestResponse);

                    Log.Warning(e, "unhandled error occurred in request response middleware");
                }
            }
        }

        /// <summary>
        /// Run notification middleware for all enqueued notifications.
        /// </summary>
        private void RunNotificationMiddleware()
            // Notification ownership is moved to the application at this point.
            => notificationCenter.Handle((notification) =>
            {
                // All notifications might not have peers associated with them. In this case we broadcast the notification to all peers.
                var peerIds = (notification.PeerIds ?? server.PeerIds).ToArray();

                try
                {
                    // Filter all requests that are rejected by the middleware.
                    if (notificationMiddleware.Invoke(new NotificationMiddlewareContext(peerIds, notification)))
                        ReleaseNotification(notification);
                    else
                        acceptedNotifications.Enqueue(notification);
                }
                catch (Exception e)
                {
                    foreach (var peerId in peerIds)
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Notification, peerId);

                    HandlePipelineExceptionFidelity(e);

                    ReleaseNotification(notification);

                    Log.Warning(e, "unhandled error occurred in notification middleware");
                }
            });

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

                    server.Send(requestResponse.Request.Connection.PeerId, contents, 0, size);
                }
                catch (Exception e)
                {
                    HandlePeerFidelityViolation(PeerPipelineFidelity.Response, requestResponse.Request.Connection.PeerId);

                    HandlePipelineExceptionFidelity(e);

                    Log.Warning(e, "unhandled error occurred while handling accepted responses");
                }

                ReleaseRequestResponse(requestResponse);
            }
        }

        /// <summary>
        /// Send out all enqueued notifications.
        /// </summary>
        private void HandleAcceptedNotifications()
        {
            void Send(in IMessage message, int peerId)
            {
                var size     = serializer.GetSizeFromMessage(message);
                var contents = BufferPool.Take(size);

                serializer.Serialize(message, contents, 0);

                server.Send(peerId, contents, 0, size);
            }

            void Broadcast(in IMessage message, int[] peerIds)
            {
                var size     = serializer.GetSizeFromMessage(message);
                var contents = BufferPool.Take(size);

                serializer.Serialize(message, contents, 0);

                foreach (var peerId in peerIds)
                {
                    var copy = BufferPool.Take(size);

                    contents.CopyTo(copy, 0);

                    server.Send(peerId, copy, 0, size);
                }

                BufferPool.Return(contents);
            }

            void Reset(in IMessage message, int[] peerIds)
            {
                if (message != null)
                    Broadcast(message, peerIds);

                foreach (var peerId in peerIds)
                    leavingPeerIds.Add(peerId);
            }

            while (acceptedNotifications.Count != 0)
            {
                var notification = acceptedNotifications.Dequeue();
                var peerIds      = (notification.PeerIds ?? server.PeerIds).Where(p => !leavingPeerIds.Contains(p)).ToArray();

                try
                {
                    // Handle each notification based on the command associated with them. Filter out any peers that the handler
                    // might have mark for reset.
                    switch (notification.Command)
                    {
                        case NotificationCommand.Send:
                            var peerId = peerIds.First();

                            if (!leavingPeerIds.Contains(peerId))
                                Send(notification.Message, peerId);

                            break;
                        case NotificationCommand.BroadcastNarrow:
                            Broadcast(notification.Message, peerIds);

                            break;
                        case NotificationCommand.BroadcastWide:
                            Broadcast(notification.Message, peerIds);

                            break;
                        case NotificationCommand.Reset:
                            Reset(notification.Message, peerIds);

                            break;
                        case NotificationCommand.Shutdown:
                            Reset(notification.Message, peerIds);

                            break;
                        default:
                            Log.Error("notification command was unset or unsupported", notification);

                            break;
                    }
                }
                catch (Exception e)
                {
                    foreach (var peerId in peerIds)
                        HandlePeerFidelityViolation(PeerPipelineFidelity.Notification, peerId);

                    HandlePipelineExceptionFidelity(e);

                    Log.Warning(e, "unhandled error occurred while handling accepted notifications");
                }

                ReleaseNotification(notification);
            }
        }

        /// <summary>
        /// Resets (disconnects) all peers that have been marked as leaving.
        /// </summary>
        private void ResetLeavingPeers()
        {
            foreach (var peerId in leavingPeerIds)
                try
                {
                    server.Disconnect(peerId);
                }
                catch (Exception e)
                {
                    Log.Error(e, "unhandled error occurred while disconnecting peer");

                    HandlePipelineExceptionFidelity(e);
                }

            leavingPeerIds.Clear();
        }

        public void Shutdown()
        {
            if (!running)
                throw new InvalidOperationException("already running");

            Log.Information("application shutdown signaled");

            running = false;
        }

        public void Start()
        {
            if (running)
                throw new InvalidOperationException("already running");

            Log.Information("starting application...");

            Initialize();

            running = true;

            Log.Information("entering application event loop");

            while (running)
            {
                applicationLoopTimer.Begin();

                // Step 1: poll server and receive events. Handle all incoming peer leave and join events first and track all peers that
                //         are about to leave.
                server.Poll();

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

                // Step 7: update application timer.
                timer.Tick();

                applicationLoopTimer.End(() =>
                {
                    Log.Debug($"{applicationLoopTimer.Name} metrics: min: {applicationLoopTimer.Min.TotalMilliseconds}ms - " +
                              $"avg: {applicationLoopTimer.Average.TotalMilliseconds}ms - " +
                              $"max: {applicationLoopTimer.Max.TotalMilliseconds}ms");
                });
            }

            Log.Information("exited application event loop");

            Deinitialize();
        }
    }
}