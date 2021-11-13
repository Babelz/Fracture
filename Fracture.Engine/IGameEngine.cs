using System;
using Fracture.Common.Di;
using Fracture.Engine.Core;

namespace Fracture.Engine
{
    /// <summary>
    /// Interface for implementing games and game engines. Engines contain
    /// initialization logic and bound all systems together.
    /// </summary>
    public interface IGameEngine
    {
        #region Events
        event EventHandler Exiting;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the time of the engine.
        /// </summary>
        IGameEngineTime Time
        {
            get;
        }
        
        /// <summary>
        /// Gets service locator for getting non-system.
        /// </summary>
        IDependencyLocator Services
        {
            get;
        }
        #endregion
        
        void Exit();
    }
}
