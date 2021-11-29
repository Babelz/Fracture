using System;
using System.Collections.Generic;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using NLog;

namespace Fracture.Net.Hosting
{
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
        
        private readonly IObjectActivator activator;
        #endregion
        
        public ApplicationScriptManager(IObjectActivator activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
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
                    Log.Warn(e, "unahdled error occurred while executing command script", script);
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
                    Log.Warn(e, "unhandled error occurred while executing active script", activeScripts);
                }
            }
                
            unloadedActiveScripts.ForEach(s => activeScripts.Remove(s));
            unloadedActiveScripts.Clear();
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
                    
                    Log.Info($"loaded command script: {type.Name}...");
                    break;
                case IActiveApplicationScript ias:
                    newActiveScripts.Add(ias);
                    
                    ias.Unloading += delegate
                    {
                        unloadedActiveScripts.Add(ias);
                    };
                    
                    Log.Info($"loaded active script: {type.Name}...");
                    break;
                default:
                    Log.Info($"loaded script: {type.Name}...");
                    break;
            }
        }
        
        public void Load<T>(params IBindingValue[] args) where T : class, IApplicationScript
            => Load(typeof(T), args);

        public void Tick()
        {
            newCommandScripts.ForEach(commandScripts.Enqueue);
            newActiveScripts.ForEach(activeScripts.Add);
            
            newCommandScripts.Clear();
            newActiveScripts.Clear();
            
            RunCommandScripts();
            
            UpdateActiveScripts();
        }
    }
}