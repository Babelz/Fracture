using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        /// Clears session object from given peer.
        /// </summary>
        void Clear(int id);

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
    
    public sealed class ClearSessionScript<T> : ApplicationScript where T : Session
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        [BindingConstructor]
        public ClearSessionScript(IApplicationScriptingHost application, ISessionService<T> sessions, int id) 
            : base(application)
        {
            Log.Info($"session auto clearing setup for peer {id}");
            
            void ClearSession(object sender, in PeerResetEventArgs args)
            {
                if (args.Peer.Id != id) 
                    return;
                
                Log.Info($"clearing session for peer {id}", sessions.Get(id));
                
                sessions.Clear(id);
                
                Application.Reset -= ClearSession;
            }
            
            application.Reset += ClearSession;
        }
    }
}