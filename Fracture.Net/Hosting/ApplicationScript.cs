using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
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
    
    public readonly struct StartupScriptContext
    {
        #region Properties
        public Type Type
        {
            get;
        }

        public IBindingValue[] Args
        {
            get;
        }
        #endregion

        public StartupScriptContext(Type type, params IBindingValue[] args)
        {
            Type = type ?? throw new ArgumentException(nameof(type));
            Args = args;
        }
    }
    
    /// <summary>
    /// Interface that provides functionality for loading new scripts during runtime inside the application.
    /// </summary>
    public interface IApplicationScriptLoader
    {
        void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript;
        
        void Load(Type type, params IBindingValue[] args);
    }
    
    /// <summary>
    /// Interface that provides functionality required for application level script management.
    /// </summary>
    public interface IApplicationScriptManager : IApplicationScriptLoader
    {
        void Initialize(IObjectActivator activator);

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
        
        private readonly IEnumerable<StartupScriptContext> startupScriptContexts;
        
        private IObjectActivator activator;
        #endregion
        
        public ApplicationScriptManager(IEnumerable<StartupScriptContext> startupScriptContexts)
        {
            this.startupScriptContexts = startupScriptContexts ?? throw new ArgumentNullException(nameof(startupScriptContexts));
            
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

        public void Initialize(IObjectActivator activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
            foreach (var startupContext in startupScriptContexts)
                Load(startupContext.Type, startupContext.Args);
        }
            
        public void Load(Type type, params IBindingValue[] args)
        {
            if (!typeof(IApplicationScript).IsAssignableFrom(type))
                throw new ArgumentException($"{type.Name} is not a script type", nameof(type));
            
            if (type.IsAbstract || type.IsInterface)
                throw new ArgumentException($"{type.Name} is abstract", nameof(type));
            
            if (type.IsValueType)
                throw new ArgumentException($"{type.Name} is a value type", nameof(type)); 
            
            var script = activator.Activate(type, args);
            
            switch (script)
            {
                case ICommandApplicationScript ics:
                    newCommandScripts.Add(ics);
                    break;
                case IActiveApplicationScript ias:
                    newActiveScripts.Add(ias);
                    
                    ias.Unloading += delegate
                    {
                        unloadedActiveScripts.Add(ias);
                    };
                    break;
                default:
                    throw new InvalidOrUnsupportedException("script type", script.GetType());
            }
        }
        
        public void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript
            => Load(typeof(T), args);

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