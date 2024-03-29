﻿using System;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Abstract base class for implementing CS scripts. 
    /// </summary>
    public abstract class CsScript
    {
        #region Events
        /// <summary>
        /// Event invoked when the script is being unloaded.
        /// </summary>
        public event EventHandler Unloading;

        /// <summary>
        /// Event invoked when the script is being loaded.
        /// </summary>
        public event EventHandler Loading;
        #endregion

        /// <summary>
        /// Creates new instance of script. Use <see cref="BindingConstructorAttribute"/> to parametrize script activation.
        /// activation.
        /// </summary>
        protected CsScript()
        {
        }

        /// <summary>
        /// Method called when the script is getting loaded. This happens right after the script has been loaded and
        /// happens only once during the scripts life time.
        /// </summary>
        public virtual void Load()
            => Loading?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Method called when the script is getting unloaded. This happens right before the script is getting disposed
        /// and happens only once during the scripts life time.
        /// </summary>
        public virtual void Unload()
            => Unloading?.Invoke(this, EventArgs.Empty);
    }

    public abstract class CommandCsScript : CsScript
    {
        protected CommandCsScript()
        {
        }

        /// <summary>
        /// Method invoke once for the script before it is unloaded.
        /// </summary>
        public abstract void Execute(IGameEngineTime time);
    }

    public abstract class ActiveCsScript : CsScript
    {
        protected ActiveCsScript()
        {
        }

        /// <summary>
        /// Method called every frame to allow the script run custom update logic.
        /// </summary>
        public abstract void Update(IGameEngineTime time);
    }
}