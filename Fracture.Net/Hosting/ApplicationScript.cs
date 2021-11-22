using System;
using System.Collections.Generic;
using Fracture.Common;
using Fracture.Common.Di.Attributes;
using NLog;

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
    
    public interface IApplicationScriptManager
    {
        void Add(IApplicationScript script);
        
        void Tick();
    }
    
    public sealed class ApplicationScriptManager : IApplicationScriptManager
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly List<ICommandApplicationScript> newCommandScripts;
        private readonly List<IActiveApplicationScript> newActiveScripts;
        
        private readonly Queue<ICommandApplicationScript> commandScripts;
        private readonly List<IActiveApplicationScript> activeScripts;
        
        private readonly List<IActiveApplicationScript> unloadedActiveScripts;
        #endregion
        
        public ApplicationScriptManager()
        {
            commandScripts        = new Queue<ICommandApplicationScript>();
            activeScripts         = new List<IActiveApplicationScript>();
            unloadedActiveScripts = new List<IActiveApplicationScript>();

            newCommandScripts = new List<ICommandApplicationScript>();
            newActiveScripts  = new List<IActiveApplicationScript>();
        }

        private void RunCommandScripts()
        {
            while (commandScripts.Count != 0)
            {
                var script = commandScripts.Dequeue();
                
                try
                {
                    script.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e, "error occurred while executing command script", script);
                }
            }
        }
        
        private void UpdateActiveScripts()
        {
            foreach (var script in activeScripts)
            {
                try
                {
                    script.Tick();
                }
                catch (Exception e)
                {
                    Log.Error(e, "error occurred while executing active script", activeScripts);
                }
            }
                
            unloadedActiveScripts.ForEach(s => activeScripts.Remove(s));
            unloadedActiveScripts.Clear();
        }
        
        public void Add(IApplicationScript script)
        {
            switch (script)
            {
                case ICommandApplicationScript ics:
                    newCommandScripts.Add(ics);
                    break;
                case IActiveApplicationScript ias:
                    newActiveScripts.Add(ias);
                    
                    script.Unloading += delegate
                    {
                        unloadedActiveScripts.Add(ias);
                    };
                    break;
                default:
                    throw new InvalidOrUnsupportedException("script type", script.GetType());
            }
        }

        public void Tick()
        {
            RunCommandScripts();
            
            UpdateActiveScripts();
            
            newCommandScripts.ForEach(commandScripts.Enqueue);
            newActiveScripts.ForEach(activeScripts.Add);
            
            newCommandScripts.Clear();
            newActiveScripts.Clear();
        }
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