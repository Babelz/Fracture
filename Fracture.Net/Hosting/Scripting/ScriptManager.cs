using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using Fracture.Common;
using Fracture.Common.Di;
using Fracture.Common.Di.Binding;

namespace Fracture.Net.Hosting.Scripting
{
    public interface IScriptHost
    {
        void Load<T>(IScriptActivationArgs args) where T : IScript;
    }
    
    public interface IScriptManager : IScriptHost
    {
        void Tick(IApplicationClock clock);
    }
    
    public sealed class ScriptManager : IScriptManager
    {
        #region Fields
        private readonly IObjectActivator activator;
        
        private readonly List<ICommandScript> commandScripts;
        private readonly List<IActiveScript> activeScripts;
        
        private readonly List<IActiveScript> unloadedActiveScripts;
        #endregion
        
        public ScriptManager(IObjectActivator activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
            commandScripts        = new List<ICommandScript>();
            activeScripts         = new List<IActiveScript>();
            unloadedActiveScripts = new List<IActiveScript>();
        }

        #region Event handlers
        private void Script_OnUnloading(object sender, EventArgs e)
        {
            switch (sender)
            {
                case ICommandScript ics:
                    commandScripts.Remove(ics);
                    break;
                case IActiveScript ias:
                    unloadedActiveScripts.Add(ias);
                    break;
                default:
                    throw new InvalidOrUnsupportedException("script type", sender.GetType());
            }
        }
        #endregion
        
        private void RunCommandScripts(IApplicationClock clock)
        {
            while (commandScripts.Count != 0)
                commandScripts[0].Invoke(clock);
        }
        
        private void UpdateActiveScripts(IApplicationClock clock)
        {
            for (var i = 0; i < activeScripts.Count; i++)
                activeScripts[i].Tick(clock);
            
            while (unloadedActiveScripts.Count != 0)
            {
                activeScripts.Remove(unloadedActiveScripts[0]);
                
                unloadedActiveScripts.RemoveAt(0);
            }
        }
        
        public void Load<T>(IScriptActivationArgs args) where T : IScript
        {
            var script = activator.Activate<T>(BindingValue.Const(nameof(args), args));
            
            switch (script)
            {
                case ICommandScript ics:
                    commandScripts.Add(ics);
                    break;
                case IActiveScript ias:
                    activeScripts.Add(ias);
                    break;
                default:
                    throw new InvalidOrUnsupportedException("script type", typeof(T));
            }
            
            script.Unloading += Script_OnUnloading;
        }

        public void Tick(IApplicationClock clock)
        {
            RunCommandScripts(clock);
            
            UpdateActiveScripts(clock);
        }
    }
}