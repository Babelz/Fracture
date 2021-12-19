using System;
using System.Collections.Generic;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Util;
using Fracture.Net.Hosting.Messaging;
using Fracture.Net.Hosting.Servers;
using Newtonsoft.Json;
using NLog;

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
        bool Active(int id);
        
        /// <summary>
        /// Clears session object from given peer.
        /// </summary>
        void Clear(int id);
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
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService<T> : ISessionService where T : SessionBase
    {
        /// <summary>
        /// Creates or updates session of specific peer.
        /// </summary>
        void Update(int peer, T session);

        /// <summary>
        /// Attempts to get the session for given session id and returns boolean declaring whether a active session could be retrieved for the peer.
        /// </summary>
        bool TryGet(int id, out T session);
        
        /// <summary>
        /// Gets session for peer with given id.
        /// </summary>
        T Get(int id);
    }
    
    public static class SessionServiceMiddleware<T> where T : SessionBase
    {
        public static MiddlewareHandlerDelegate<RequestMiddlewareContext> CreateRefreshSession(ISessionService<T> sessions)
            => (in RequestMiddlewareContext context) =>
            {
                if (sessions.TryGet(context.Request.Peer.Id, out var session))
                    session.Refresh();
                
                return MiddlewareHandlerResult.Accept;  
            };
    }

    /// <summary>
    /// Default implementation of <see cref="ISessionService"/>.
    /// </summary>
    public class SessionService<T> : ApplicationService, ISessionService<T> where T : SessionBase
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly Dictionary<int, T> sessions;
        #endregion
        
        [BindingConstructor]
        public SessionService(IApplicationServiceHost application) 
            : base(application)
        {
            sessions = new Dictionary<int, T>();
        }

        public bool Active(int id)
            => sessions.ContainsKey(id);

        public void Update(int peer, T session)
        {
            if (!Active(peer))
                sessions.Add(peer, session ?? throw new ArgumentNullException(nameof(session)));
            else
                sessions[peer] = session;
        }
        
        public void Clear(int id)
        {
            if (!sessions.TryGetValue(id, out var session))
                return;
            
            sessions.Remove(id);
        }
            
        public bool TryGet(int id, out T session)
            => sessions.TryGetValue(id, out session);
        
        public T Get(int id)
            => sessions[id];
    }
    
    /// <summary>
    /// Script that automatically clears peer session when the peer resets.
    /// </summary>
    public sealed class ClearSessionScript : ApplicationScript
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

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
            if (!sessions.Active(e.Peer.Id))
                return;
            
            Log.Info($"clearing session for peer {e.Peer.Id}");
                
            sessions.Clear(e.Peer.Id);
        }
        #endregion
    }
    
    /// <summary>
    /// Script that authenticates new connections by creating new sessions for them.
    /// </summary>
    public sealed class SessionAuthenticateScript<T> : ApplicationScript where T : SessionBase, new()
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        [BindingConstructor]
        public SessionAuthenticateScript(IApplicationScriptingHost application, ISessionService<T> sessions) 
            : base(application)
        {
            if (sessions == null)
                throw new ArgumentNullException(nameof(sessions));
            
            application.Join += (object sender, in PeerJoinEventArgs e) =>
            {
                Log.Info($"new peer {e.Peer.Id} connected, creating session");
                
                sessions.Update(e.Peer.Id, new T());
            };
        }
    }
}