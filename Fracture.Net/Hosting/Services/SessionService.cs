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
    public abstract class Session
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

        protected Session()
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
            => JsonConvert.ToString(this);

        public override int GetHashCode()
            => HashUtils.Create().Append(Created);
    }
    
    /// <summary>
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService<T> : ISessionService where T : Session
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
    
    public static class SessionServiceMiddleware<T> where T : Session
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
    public class SessionService<T> : ApplicationService, ISessionService<T> where T : Session
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
            {
                Log.Info($"creating session for peer {peer}", session);
                
                sessions.Add(peer, session ?? throw new ArgumentNullException(nameof(session)));
            }
            else
            {
                Log.Info($"updating session for peer {peer}", session);
                
                sessions[peer] = session;
            }
        }
        
        public void Clear(int id)
        {
            if (!sessions.TryGetValue(id, out var session))
                return;
            
            Log.Info($"clearing session for peer {id}", session);
                
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
}