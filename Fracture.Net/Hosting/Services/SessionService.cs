using System;
using System.Collections;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Util;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Newtonsoft.Json;
using Serilog;

namespace Fracture.Net.Hosting.Services
{
    /// <summary>
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService : IApplicationService
    {
        /// <summary>
        /// Returns boolean declaring whether given peer id has session associated with it. 
        /// </summary>
        bool Active(int peerId);

        /// <summary>
        /// Clears session object from given peer.
        /// </summary>
        void Clear(int peerId);
    }

    /// <summary>
    /// Abstract base class for implementing sessions.
    /// </summary>
    public abstract class SessionBase
    {
        #region Properties
        /// <summary>
        /// Gets the timestamp when the session was created.
        /// </summary>
        public DateTime Created
        {
            get;
        }

        /// <summary>
        /// Gets last timestamp the session was refreshed. 
        /// </summary>
        public DateTime Refreshed
        {
            get;
            private set;
        }
        #endregion

        protected SessionBase()
        {
            Created   = DateTime.UtcNow;
            Refreshed = Created;
        }

        /// <summary>
        /// Refresh the session and update refreshed timestamp.
        /// </summary>
        public virtual void Refresh()
            => Refreshed = DateTime.UtcNow;

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        public override int GetHashCode()
            => HashUtils.Create().Append(Created);
    }

    /// <summary>
    /// Delegate for aggregating session objects.
    /// </summary>
    public delegate T SessionAggregatorDelegate<T>(in T current) where T : SessionBase;

    /// <summary>
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService<T> : ISessionService, IEnumerable<T> where T : SessionBase
    {
        /// <summary>
        /// Creates or updates session of specific peer.
        /// </summary>
        void Update(int peerId, T session);

        /// <summary>
        /// Updates session of specific peer using aggregator delegate.
        /// </summary>
        T Update(int peerId, SessionAggregatorDelegate<T> aggregator);

        /// <summary>
        /// Attempts to get the session for given session id and returns boolean declaring whether a active session could be retrieved for the peer.
        /// </summary>
        bool TryGet(int peerId, out T session);

        /// <summary>
        /// Gets session for peer with given id.
        /// </summary>
        T Get(int peerId);
    }

    public static class SessionServiceMiddleware<T> where T : SessionBase
    {
        public static MiddlewareHandlerDelegate<RequestMiddlewareContext> CreateRefreshSession(ISessionService<T> sessions)
            => (in RequestMiddlewareContext context) =>
            {
                if (sessions.TryGet(context.Request.Connection.PeerId, out var session))
                    session.Refresh();

                return MiddlewareHandlerResult.Accept;
            };
    }

    /// <summary>
    /// Default implementation of <see cref="ISessionService"/>.
    /// </summary>
    public class SessionService<T> : ApplicationService, ISessionService<T> where T : SessionBase
    {
        #region Fields
        private readonly Dictionary<int, T> sessions;
        #endregion

        [BindingConstructor]
        public SessionService(IApplicationServiceHost application)
            : base(application)
            => sessions = new Dictionary<int, T>();

        public bool Active(int peerId)
            => sessions.ContainsKey(peerId);

        public void Update(int peerId, T session)
        {
            if (!Active(peerId))
                sessions.Add(peerId, session ?? throw new ArgumentNullException(nameof(session)));
            else
                sessions[peerId] = session;
        }

        public T Update(int peerId, SessionAggregatorDelegate<T> aggregator)
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));

            if (!sessions.TryGetValue(peerId, out var currentSession))
                throw new InvalidOperationException($"unable to patch session for peer {peerId}, no past session exists");

            var newSession = aggregator(currentSession);

            sessions[peerId] = newSession;

            return newSession;
        }

        public void Clear(int peerId)
            => sessions.Remove(peerId);

        public bool TryGet(int peerId, out T session)
            => sessions.TryGetValue(peerId, out session);

        public T Get(int peerId)
            => sessions[peerId];

        public IEnumerator<T> GetEnumerator()
            => sessions.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    /// <summary>
    /// Script that automatically clears peer session when the peer resets.
    /// </summary>
    public sealed class ClearSessionScript : ApplicationScript
    {
        #region Fields
        private readonly ISessionService sessions;
        #endregion

        [BindingConstructor]
        public ClearSessionScript(IApplicationScriptingHost application, ISessionService sessions)
            : base(application)
        {
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));

            application.Reset += Application_OnReset;
        }

        #region Event handlers
        private void Application_OnReset(object sender, in PeerResetEventArgs e)
        {
            if (!sessions.Active(e.Connection.PeerId))
                return;

            Log.Information($"clearing session for peer {e.Connection.PeerId}");

            sessions.Clear(e.Connection.PeerId);
        }
        #endregion
    }

    /// <summary>
    /// Script that authenticates new connections by creating new sessions for them.
    /// </summary>
    public sealed class SessionAuthenticateScript<T> : ApplicationScript where T : SessionBase, new()
    {
        [BindingConstructor]
        public SessionAuthenticateScript(IApplicationScriptingHost application, ISessionService<T> sessions)
            : base(application)
        {
            if (sessions == null)
                throw new ArgumentNullException(nameof(sessions));

            application.Join += (object sender, in PeerJoinEventArgs e) =>
            {
                Log.Information($"new peer {e.Connection.PeerId} connected, creating session");

                sessions.Update(e.Connection.PeerId, new T());
            };
        }
    }
}