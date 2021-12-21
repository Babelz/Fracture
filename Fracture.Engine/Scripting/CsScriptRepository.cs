using System;
using System.Collections;
using System.Collections.Generic;
using Fracture.Common;
using Fracture.Common.Collections;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Common.Reflection;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Interface for implementing CS script repository factories that
    /// are responsible for creating script repositories. Repositories
    /// are created based on the attributes on the given script type.
    /// </summary>
    public interface ICsScriptRepositoryFactory
    {
        ICsScriptRepository<T> Create<T>(IGameHost host) where T : CsScript;
    }
    
    /// <summary>
    /// Default implementation of <see cref="ICsScriptRepositoryFactory"/>.
    /// </summary>
    public sealed class CsScriptRepositoryFactory : ICsScriptRepositoryFactory
    {
        public CsScriptRepositoryFactory()
        {
        }

        public ICsScriptRepository<T> Create<T>(IGameHost host) where T : CsScript
        {
            if (!CsScriptTypeLoader.TryGetScriptDefinitionAttribute(typeof(T), out var definition))
                throw new MissingAttributeException(typeof(T), typeof(CsScriptDefinitionAttribute));

            if (definition.Unique)
                return new UniqueCsScriptRepository<T>(host);

            if (definition.Reusable)
                return new ReusableCsScriptRepository<T>(host);
            
            return new DisposableCsScriptRepository<T>(host);
        }
    }
    
    public delegate T ScriptActivator<out T>(IGameHost host, string name) where T : CsScript;
    
    /// <summary>
    /// Interface for implementing CS script repositories. Repositories
    /// are fine tuned to handle different situations for example
    /// scripts that are frequently used can be pooled.
    /// </summary>
    public interface ICsScriptRepository<T> : IEnumerable<T> where T : CsScript
    {
        bool TryLoad(string name, out T script);

        /// <summary>
        /// Attempts to load script with given name, throws
        /// exception in case no script can be loaded.
        /// </summary>
        T Load(string name);
    }
        
    /// <summary>
    /// CS Script repository that guarantees new, unique instance to be created and allocated
    /// each time Load is called. Unloaded scripts are released and allowed to be collected
    /// by GC.
    /// </summary>
    public sealed class DisposableCsScriptRepository<T> : ICsScriptRepository<T> where T : CsScript
    {
        #region Fields
        private readonly List<T> scripts;

        private readonly Dictionary<string, ScriptActivator<T>> activators;

        private readonly IGameHost host;
        #endregion

        public DisposableCsScriptRepository(IGameHost host)
        {
            if (!CsScriptTypeLoader.TryGetScriptDefinitionAttribute(typeof(T), out var definition))
                throw new MissingAttributeException(typeof(T), typeof(CsScriptDefinitionAttribute));

            this.host = host ?? throw new ArgumentNullException(nameof(host));

            activators = new Dictionary<string, ScriptActivator<T>>();
            scripts    = new List<T>();

            var types = CsScriptTypeLoader.GetScriptTypes(definition);
            
            foreach (var type in types)
            {
                if (!CsScriptTypeLoader.TryGetScriptAttribute(type, out var script))
                    throw new MissingAttributeException(typeof(T), typeof(CsScriptAttribute));

                var ctor      = type.GetConstructor(new[] { typeof(IGameHost), typeof(string) });
                var activator = (ScriptActivator<T>)DynamicConstructorBinder.Bind(ctor, typeof(ScriptActivator<>).MakeGenericType(type));
                
                activators.Add(script.Name, activator);
            }
        }

        #region Event handlers
        private void Script_Unloaded(object sender, EventArgs e)
        {
            var script = (T)sender;

            script.Unloaded -= Script_Unloaded;

            // Just call unload and that's it, allow GC to
            // collect this object.
            script.OnUnload();

            scripts.Remove(script);
        }
        #endregion

        public T Load(string name)
        {
            if (!TryLoad(name, out var script))
                throw new InvalidOperationException($"script {name} does not exist");
            
            return script;
        }
        
        public bool TryLoad(string name, out T script)
        {
            if (!activators.TryGetValue(name, out var activator))
                script = default;
            else
            {
                script = activator(host, name);

                script.Unloaded += Script_Unloaded;

                scripts.Add(script);

                script.OnLoad();
            }

            return script != default;
        }

        public IEnumerator<T> GetEnumerator()
            => scripts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
        
    /// <summary>
    /// CS script repository that pools scripts and reuses them instead of 
    /// allowing GC to collect them right after they are unloaded.
    /// </summary>
    public sealed class ReusableCsScriptRepository<T> : ICsScriptRepository<T> where T : CsScript
    {
        #region Fields
        private readonly List<T> scripts;

        private readonly Dictionary<string, IPool<T>> pools;
        #endregion

        public ReusableCsScriptRepository(IGameHost host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (!CsScriptTypeLoader.TryGetScriptDefinitionAttribute(typeof(T), out var definition))
                throw new MissingAttributeException(typeof(T), typeof(CsScriptDefinitionAttribute));

            pools   = new Dictionary<string, IPool<T>>();
            scripts = new List<T>();

            var types = CsScriptTypeLoader.GetScriptTypes(definition);

            foreach (var type in types)
            {
                if (!CsScriptTypeLoader.TryGetScriptAttribute(type, out var script))
                    throw new MissingAttributeException(typeof(T), typeof(CsScriptAttribute));

                var ctor      = type.GetConstructor(new[] { typeof(IGameHost), typeof(string) });
                var activator = (ScriptActivator<T>)DynamicConstructorBinder.Bind(ctor, typeof(ScriptActivator<>).MakeGenericType(type));
                
                pools.Add(script.Name, 
                    new LinearPool<T>(
                        new LinearStorageObject<T>(
                            new LinearGrowthArray<T>(
                                definition.Allocations)), () => activator(host, script.Name), definition.Allocations));
            }
        }
        
        #region Event handlers
        private void Script_Unloaded(object sender, EventArgs e)
        {
            var script = (T)sender;

            script.Unloaded -= Script_Unloaded;

            script.OnUnload();

            // Return script to pool for reuse.
            // pools[script.Name].Return(script);

            scripts.Remove(script);
        }
        #endregion

        public T Load(string name)
        {
            if (!TryLoad(name, out var script))
                throw new InvalidOperationException($"script {name} does not exist");
            
            return script;
        }
        
        public bool TryLoad(string name, out T script)
        {
            if (!pools.TryGetValue(name, out var pool))
                script = default;
            else
            {
                script = pool.Take();

                script.Unloaded += Script_Unloaded;

                scripts.Add(script);

                script.OnLoad();
            }

            return script != default;
        }

        public IEnumerator<T> GetEnumerator()
            => scripts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
        
    /// <summary>
    /// CS Script repository that allows once instance of each script to exist at
    /// any given time.
    /// </summary>
    public sealed class UniqueCsScriptRepository<T> : ICsScriptRepository<T> where T : CsScript
    {
        #region Fields
        private readonly Dictionary<string, T> scripts;
        private readonly Dictionary<string, int> loads;
        
        private readonly List<T> loaded;
        #endregion
        
        public UniqueCsScriptRepository(IGameHost host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (!CsScriptTypeLoader.TryGetScriptDefinitionAttribute(typeof(T), out var definition))
                throw new MissingAttributeException(typeof(T), typeof(CsScriptDefinitionAttribute));

            loaded  = new List<T>();
            scripts = new Dictionary<string, T>();
            loads   = new Dictionary<string, int>();

            var types = CsScriptTypeLoader.GetScriptTypes(definition);

            foreach (var type in types)
            {
                if (!CsScriptTypeLoader.TryGetScriptAttribute(type, out var script))
                    throw new MissingAttributeException(typeof(T), typeof(CsScriptAttribute));

                var instance = (T)Activator.CreateInstance(type, host, script.Name);

                scripts.Add(script.Name, instance);

                loads.Add(script.Name, 0);
            }
        }

        #region Event handlers
        private void Script_Unloaded(object sender, EventArgs e)
        {
            var script = (T)sender;

            script.Unloaded -= Script_Unloaded;

            throw new NotImplementedException(); //loads[script.Name] -= 1; 

            // if (loads[script.Name] == 0)
            //     script.OnUnload();

            loaded.Remove(script);
        }
        #endregion

        public T Load(string name)
        {
            if (!TryLoad(name, out var script))
                throw new InvalidOperationException($"script {name} does not exist");

            return script;
        }
        
        public bool TryLoad(string name, out T script)
        {
            if (!scripts.TryGetValue(name, out script)) 
                return false;
            
            if (loads[name] == 0)
            {
                script.OnLoad();

                loads[name] += 1;
            }

            script.Unloaded += Script_Unloaded;

            loaded.Add(script);

            return script != default;
        }

        public IEnumerator<T> GetEnumerator()
            => loaded.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => loaded.GetEnumerator();
    }
}