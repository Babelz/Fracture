using System;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Abstract base class for implementing CS scripts. 
    /// </summary>
    public abstract class CsScript
    {
        /// <summary>
        /// Creates new instance of script. Use <see cref="BindingConstructorAttribute"/> for parametrized script
        /// activation.
        /// </summary>
        protected CsScript()
        {
        }

        /// <summary>
        /// Method called when the script is getting loaded. This happens right after the script has been loaded and
        /// happens only once during the scripts life time. 
        /// 
        /// When overriding, no need to call base method.
        /// </summary>
        public virtual void Load()
        {
        }

        /// <summary>
        /// Method called when the script is getting unloaded. This happens right before the script is getting disposed
        /// and happens only once during the scripts life time.
        /// 
        /// When overriding, no need to call base method.
        /// </summary>
        public virtual void Unload()
        {
        }
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
