using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;

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
        bool Exists(int id);
    }
    
    /// <summary>
    /// Interface for implementing services that provide peer session management for application.
    /// </summary>
    public interface ISessionService<T> : ISessionService where T : class
    {
        /// <summary>
        /// Creates and associates given session object with given peer.
        /// </summary>
        void Create(int id, T session);
        
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
    public class SessionService<T> : ApplicationService, ISessionService<T> where T : class
    {
        #region Fields
        private readonly Dictionary<int, T> sessions;
        #endregion
        
        [BindingConstructor]
        public SessionService(IApplicationServiceHost application) 
            : base(application)
        {
            sessions = new Dictionary<int, T>();
        }

        public bool Exists(int id)
            => sessions.ContainsKey(id);

        public void Create(int id, T session)
            => sessions.Add(id, session ?? throw new ArgumentNullException(nameof(session)));

        public void Clear(int id)
            => sessions.Remove(id);

        public bool TryGet(int id, out T session)
            => sessions.TryGetValue(id, out session);
    }
}