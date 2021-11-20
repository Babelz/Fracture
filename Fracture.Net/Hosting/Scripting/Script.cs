using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Net.Hosting.Scripting
{
    /// <summary>
    /// Interface for implementing various scripts. In Fracture scripts and services provide different levels for application programming. Where services
    /// contain the framework code, scripts contain the application specific business logic. Scripts can consume services found in the application and load
    /// new scripts at will, but scripts should be kept isolated from each other and they should not directly communicate with each other.  
    /// </summary>
    public interface IScript
    {
        #region Events
        /// <summary>
        /// Event invoked when the script is being unloaded.
        /// </summary>
        event EventHandler Unloading;
        #endregion
    }
    
    public interface ICommandScript : IScript
    {
        void Invoke();
    }
    
    public interface IActiveScript : IScript
    {
        void Tick(); 
    }
    
    public abstract class Script : IScript
    {
        #region Fields
        private bool unloaded;
        #endregion
        
        #region Events
        public event EventHandler Unloading;
        #endregion
        
        #region Properties
        protected IApplicationScriptingHost Application
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of this script. Mark the constructor with <see cref="BindingConstructorAttribute"/> and use it to locate any dependencies.
        /// </summary>
        protected Script(IApplicationScriptingHost application)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
        }

        protected virtual void Unload()
        {
            if (unloaded)
                throw new InvalidOperationException("script is already unloaded");
            
            unloaded = true;
            
            Unloading?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public abstract class CommandScript : Script, ICommandScript
    {
        protected CommandScript(IApplicationScriptingHost application) 
            : base(application)
        {
        }

        public virtual void Invoke()
        {
            Unload();
        }
    }
    
    public abstract class ActiveScript : Script, IActiveScript
    {
        protected ActiveScript(IApplicationScriptingHost application, IScriptActivationArgs args = null) 
            : base(application, args)
        {
        }

        public abstract void Tick();
    }
}