using System;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Abstract base class for implementing CS scripts. 
    /// </summary>
    public abstract class CsScript
    {
        #region Events
        public event EventHandler Unloaded;
        #endregion
        
        protected CsScript()
        {
        }

        /// <summary>
        /// Method called when the script is getting loaded. This happens
        /// right after the script has been loaded and happens only once
        /// during the scripts life time. 
        /// 
        /// When overriding, no need to call base method.
        /// </summary>
        public virtual void OnLoad()
        {
        }

        /// <summary>
        /// Method called when the script is getting unloaded. This happens
        /// right before the script is getting disposed and happens only
        /// once during the scripts life time.
        /// 
        /// When overriding, no need to call base method.
        /// </summary>
        public virtual void OnUnload()
        {
        }

        /// <summary>
        /// Unloads the script and returns it back to the manager
        /// that created it.
        /// </summary>
        public void Unload()
        {
            Unloaded?.Invoke(this, EventArgs.Empty);

            Unloaded = null;
        }
    }
}
