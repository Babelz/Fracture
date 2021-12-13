using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;
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
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService<T> : ISessionService
    {
        /// <summary>
        /// Creates or updates session of specific peer.
        /// </summary>
        void Update(int id, T session);
        
        /// <summary>
        /// Clears session object from given peer.
        /// </summary>
        void Clear(int id);

        /// <summary>
        /// Attempts to get the session for given session id and returns boolean declaring whether a active session could be retrieved for the peer.
        /// </summary>
        bool TryGet(int id, out T session);
    }

    /// <summary>
    /// Default implementation of <see cref="ISessionService"/>.
    /// </summary>
    public class SessionService<T> : ApplicationService, ISessionService<T>
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

        public void Update(int id, T session)
        {
            if (!Active(id))
            {
                Log.Info($"creating session for peer {id}", session);
                
                sessions.Add(id, session ?? throw new ArgumentNullException(nameof(session)));
            }
            else
            {
                Log.Info($"updating session for peer {id}", session);
                
                sessions[id] = session;
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
    }
}