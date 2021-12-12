using System;
using Fracture.Common.Di.Attributes;
using NLog;

namespace Fracture.Net.Hosting
{
    /// <summary>
    /// Interface for implementing scripts. Scripts provide controller functionality for the application host model. Scripts can consume services but can't
    /// access or directly communicate with other scripts. Scripts should live in isolation from other scripts.
    ///
    /// Scripts are the controller part in any application. 
    /// </summary>
    public interface IApplicationScript
    {
        #region Events
        /// <summary>
        /// Event invoked when the script is being unloaded.
        /// </summary>
        event EventHandler Unloading;
        #endregion
        
        void Unload();
    }
    
    /// <summary>
    /// Interface for implementing command scripts. Command scripts are executed once and then unloaded.
    /// </summary>
    public interface IApplicationCommandScript : IApplicationScript
    {
        /// <summary>
        /// Invokes the script, executes any logic and unloads it.
        /// </summary>
        void Invoke();
    }
    
    /// <summary>
    /// Interface for implementing active scripts. Active scripts are allowed to run application logic on each application event loop cycle.
    /// </summary>
    public interface IActiveApplicationScript : IApplicationScript
    {
        /// <summary>
        /// Invoked once each time application loop is being executed. Allows the script to run application logic.
        /// </summary>
        void Tick(); 
    }
    
    /// <summary>
    /// Abstract base class for implementing various scripts. 
    /// </summary>
    public abstract class ApplicationScript
    {
        #region Fields
        private bool unloaded;
        #endregion
        
        #region Events
        /// <summary>
        /// Event invoked when the script is being unloaded.
        /// </summary>
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

        public void Unload()
        {
            if (unloaded)
                throw new InvalidOperationException("script is already unloaded");
            
            unloaded = true;
            
            Unloading?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Abstract base class for creating command scripts.
    /// </summary>
    public abstract class ApplicationCommandScript : ApplicationScript, IApplicationCommandScript
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        /// <summary>
        /// Creates new instance of this script. Mark the constructor with <see cref="BindingConstructorAttribute"/> and use it to locate any dependencies.
        /// </summary>
        protected ApplicationCommandScript(IApplicationScriptingHost application) 
            : base(application)
        {
        }

        protected abstract void Execute();
        
        public void Invoke()
        {
            try
            {
                Execute();
            }
            catch (Exception e)
            {
                Log.Warn(e, "unhandled exception occurred while running command script");   
            }
            
            Unload();
        }
    }
    
    /// <summary>
    /// Abstract base class for creating active scripts.
    /// </summary>
    public abstract class ActiveApplicationScript : ApplicationScript, IActiveApplicationScript
    {
        /// <summary>
        /// Creates new instance of this script. Mark the constructor with <see cref="BindingConstructorAttribute"/> and use it to locate any dependencies.
        /// </summary>
        protected ActiveApplicationScript(IApplicationScriptingHost application) 
            : base(application)
        {
        }
        
        public abstract void Tick();
    }
}