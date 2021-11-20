using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using NLog;

namespace Fracture.Net.Hosting.Scripting
{
    public interface IScriptHost
    {
        void Load<T>(params IBindingValue[] args) where T : IScript;
    }
    
    public interface IScriptManager : IScriptHost
    {
        void Tick();
    }
    
    public sealed class ScriptManager : IScriptManager
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly IObjectActivator activator;
        
        private readonly List<ICommandScript> newCommandScripts;
        private readonly List<IActiveScript> newActiveScripts;
        
        private readonly Queue<ICommandScript> commandScripts;
        private readonly List<IActiveScript> activeScripts;
        
        private readonly List<IActiveScript> unloadedActiveScripts;
        #endregion
        
        public ScriptManager(IObjectActivator activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
            commandScripts        = new Queue<ICommandScript>();
            activeScripts         = new List<IActiveScript>();
            unloadedActiveScripts = new List<IActiveScript>();

            newCommandScripts = new List<ICommandScript>();
            newActiveScripts  = new List<IActiveScript>();
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
        
        public void Load<T>(params IBindingValue[] args) where T : IScript
        {
            var script = activator.Activate<T>(args);
            
            switch (script)
            {
                case ICommandScript ics:
                    newCommandScripts.Add(ics);
                    break;
                case IActiveScript ias:
                    newActiveScripts.Add(ias);
                    
                    script.Unloading += delegate
                    {
                        unloadedActiveScripts.Add(ias);
                    };
                    break;
                default:
                    throw new InvalidOrUnsupportedException("script type", typeof(T));
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
}