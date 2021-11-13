using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Interface for creating systems that provide CS script management for CS scripts
    /// of type <see cref="T"/>. 
    /// </summary>
    public interface ICsScriptingSystem<T> : IGameEngineSystem where T : CsScript
    {
        /// <summary>
        /// Returns boolean whether script with given name is loaded.
        /// </summary>
        bool IsLoaded(string name);

        /// <summary>
        /// Returns first script with given name.
        /// </summary>
        T GetScript(string name);
        
        /// <summary>
        /// Attempts to get script. Returns boolean
        /// declaring whether a script was found.
        /// </summary>
        bool TryGetScript(string name, out T script);
        
        /// <summary>
        /// Returns all scripts with given name.
        /// </summary>
        IEnumerable<T> GetScripts(string name);

        bool TryLoad(string name, out T script);

        /// <summary>
        /// Attempts to load script with given name, throws
        /// exception in case no script can be loaded.
        /// </summary>
        T Load(string name);
    }
    
    /// <summary>
    /// Default implementation of <see cref="ICsScriptingSystem{T}"/>.
    /// </summary>
    public class CsScriptingSystem<T> : GameEngineSystem, ICsScriptingSystem<T> where T : CsScript
    {
        #region Fields
        private readonly Dictionary<string, List<T>> mappings;
        #endregion

        #region Properties
        protected ICsScriptRepository<T> Scripts
        {
            get;
        }
        #endregion

        public CsScriptingSystem(IGameEngine engine)
            : base(engine)
        {    
            Scripts = Engine.Services.First<ICsScriptRepositoryFactory>().Create<T>(Engine);

            mappings = new Dictionary<string, List<T>>();
        }

        private IEnumerable<T> GetScriptList(string name, bool create = false)
        {
            if (mappings.TryGetValue(name, out var list))
                return list;

            if (!create)
                throw new InvalidOperationException($"no scripts {name} loaded");

            list = new List<T>();

            mappings.Add(name, list);

            return list;
        }

        public T GetScript(string name)
            => GetScriptList(name).First(c => c.Name == name);

        public IEnumerable<T> GetScripts(string name)
            => GetScriptList(name).Where(c => c.Name == name);

        public bool IsLoaded(string name)
            => mappings.TryGetValue(name, out var list) && list.Any(c => c.Name == name);
        
        public T Load(string name)
            => Scripts.Load(name);
        
        public bool TryGetScript(string name, out T script)
        {
            script = default;

            if (!IsLoaded(name))
                return false;

            script = GetScript(name);

            return true;
        }

        public bool TryLoad(string name, out T script)
            => Scripts.TryLoad(name, out script);
    }
}
