using System;
using Fracture.Common.Di;
using Fracture.Engine.Core;

namespace Fracture.Engine
{
    /// <summary>
    /// Interface that provides operations for communicating with the currently running game.
    /// </summary>
    public interface IGameHost
    {
        #region Properties
        /// <summary>
        /// Gets the startup arguments passed to the game
        /// </summary>
        public string[] Args
        {
            get;
        }
        #endregion
        
        #region Events
        /// <summary>
        /// Event invoked when the game is exiting.
        /// </summary>
        event EventHandler Exiting;
        #endregion
        
        /// <summary>
        /// Signals th game to exit.
        /// </summary>
        void Exit();
    }
}
