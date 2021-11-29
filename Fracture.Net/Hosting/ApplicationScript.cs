using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface for implementing various scripts. In Fracture scripts and services provide different levels for application programming. Where services
    /// contain the framework code, scripts contain the application specific business logic. Scripts can consume services found in the application and load
    /// new scripts at will, but scripts should be kept isolated from each other and they should not directly communicate with each other.  
    /// </summary>
    public interface IApplicationScript
    {
        #region Events
        /// <summary>
        /// Event invoked when the script is being unloaded.
        /// </summary>
        event EventHandler Unloading;
        #endregion
    }
    
    public interface ICommandApplicationScript : IApplicationScript
    {
        void Invoke();
    }
    
    public interface IActiveApplicationScript : IApplicationScript
    {
        void Tick(); 
    }
    
    public abstract class ApplicationScript : IApplicationScript
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
        protected ApplicationScript(IApplicationScriptingHost application)
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
    
    public abstract class CommandApplicationScript : ApplicationScript, ICommandApplicationScript
    {
        protected CommandApplicationScript(IApplicationScriptingHost application) 
            : base(application)
        {
        }

        public virtual void Invoke()
        {
            Unload();
        }
    }
    
    public abstract class ActiveApplicationScript : ApplicationScript, IActiveApplicationScript
    {
        protected ActiveApplicationScript(IApplicationScriptingHost application) 
            : base(application)
        {
        }

        public abstract void Tick();
    }
}