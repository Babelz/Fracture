using System;

namespace Fracture.Net.Hosting.Scripting
{
    /// <summary>
    /// Interface for implementing script activation args. Use this interface for passing arguments to your scripts.
    /// </summary>
    public interface IScriptActivationArgs
    {
        // Marker interface, nothing to implement.
    }
    
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
        void Invoke(IApplicationClock clock);
    }
    
    public interface IActiveScript : IScript
    {
        void Tick(IApplicationClock clock); 
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
        
        protected IScriptActivationArgs Args
        {
            get;
        }
        #endregion

        protected Script(IApplicationScriptingHost application, IScriptActivationArgs args = null)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
            Args        = args;
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
        protected CommandScript(IApplicationScriptingHost application, IScriptActivationArgs args = null) 
            : base(application, args)
        {
        }

        public virtual void Invoke(IApplicationClock clock)
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

        public abstract void Tick(IApplicationClock clock);
    }
}