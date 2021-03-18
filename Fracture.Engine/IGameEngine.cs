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
        #region Properties
        /// <summary>
        /// Gets the time of the engine.
        /// </summary>
        IGameEngineTime Time
        {
            get;
        }
        
        /// <summary>
        /// Gets the service locator for getting services.
        /// </summary>
        IDependencyLocator Services
        {
            get;
        }
        
        /// <summary>
        /// Gets system locator for getting dependencies.
        /// </summary>
        IDependencyLocator Systems
        {
            get;
        }
        #endregion
    }
}
