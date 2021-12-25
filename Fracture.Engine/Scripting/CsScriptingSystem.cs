using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Interface for creating systems that provide CS script management for CS scripts.
    /// </summary>
    public interface ICsScriptingSystem : IGameEngineSystem
    {
        int Load<T>(params IBindingValue[] bindings) where T : CsScript;

        void Unload<T>(int id) where T : CsScript;
    }
    
    public class CsScriptingSystem : GameEngineSystem, ICsScriptingSystem
    {
        #region Static fields
        private static int idc;
        #endregion
        
        #region Fields
        private readonly Dictionary<int, CsScript> ids;
        
        private readonly IObjectActivator activator;
        
        private readonly List<CsScript> newScripts;

        private readonly List<ActiveCsScript> activeScripts;
        private readonly List<CommandCsScript> commandScripts;
        #endregion
        
        public CsScriptingSystem(IObjectActivator activator)
        {
            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
            
            ids        = new Dictionary<int, CsScript>();
            newScripts = new List<CsScript>();

            activeScripts  = new List<ActiveCsScript>();
            commandScripts = new List<CommandCsScript>();
        }

        public int Load<T>(params IBindingValue[] bindings) where T : CsScript
        {
            var id = idc++;
            var script = activator.Activate<T>(bindings);
            
            newScripts.Add(script);

            ids.Add(id, script);
            
            return id;
        }
        
        public void Unload<T>(int id) where T : CsScript
        {
        }

        public override void Update(IGameEngineTime time)
        {
        }
    }
}
