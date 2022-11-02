using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Di.Binding;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Systems;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Interface for implementing systems that provide CS script management for CS scripts.
    /// </summary>
    public interface ICsScriptingSystem : IGameEngineSystem
    {
        T Load<T>(params IBindingValue[] bindings) where T : CsScript;
    }

    /// <summary>
    /// Interface for implementing actors that react to CS scripts they support.
    /// </summary>
    public interface ICsScriptActor
    {
        /// <summary>
        /// Introduce given script to the actor and if the actor supports this type of script it will act upon it. Returns boolean declaring whether the
        /// actor was able to act on the script or not.
        /// </summary>
        bool Accept(CsScript script);

        /// <summary>
        /// Allows the actor to update its internal state.
        /// </summary>
        void Update(IGameEngineTime time);
    }

    public class CsScriptActor<T> : ICsScriptActor where T : CsScript
    {
        #region Fields
        private readonly List<T> accepted;
        private readonly List<T> unloaded;
        private readonly List<T> active;
        #endregion

        #region Properties
        protected IEnumerable<T> Active => active;
        #endregion

        public CsScriptActor()
        {
            accepted = new List<T>();
            unloaded = new List<T>();
            active   = new List<T>();
        }

        #region Event handlers
        private void Script_Unloading(object sender, EventArgs e)
            => unloaded.Add((T)sender);

        private void Script_Loading(object sender, EventArgs e)
            => active.Add((T)sender);
        #endregion

        public bool Accept(CsScript script)
        {
            if (!(script is T actual))
                return false;

            accepted.Add(actual);

            script.Unloading += Script_Unloading;
            script.Loading   += Script_Loading;

            return true;
        }

        public virtual void Update(IGameEngineTime time)
        {
            foreach (var script in accepted)
                script.Load();

            accepted.Clear();

            foreach (var script in unloaded)
                active.Remove(script);

            unloaded.Clear();
        }
    }

    public sealed class ActiveCsScriptActor : CsScriptActor<ActiveCsScript>
    {
        public ActiveCsScriptActor()
        {
        }

        public override void Update(IGameEngineTime time)
        {
            base.Update(time);

            foreach (var script in Active)
                script.Update(time);
        }
    }

    public sealed class CommandCsScriptActor : CsScriptActor<CommandCsScript>
    {
        public CommandCsScriptActor()
        {
        }

        public override void Update(IGameEngineTime time)
        {
            base.Update(time);

            foreach (var script in Active)
            {
                script.Execute(time);

                script.Unload();
            }
        }
    }

    public class CsScriptingSystem : GameEngineSystem, ICsScriptingSystem
    {
        #region Fields
        private readonly List<ICsScriptActor> actors;

        private readonly IGameObjectActivatorSystem activator;
        #endregion

        [BindingConstructor]
        public CsScriptingSystem(IGameObjectActivatorSystem activator)
        {
            this.activator = activator;

            actors = new List<ICsScriptActor>
            {
                new ActiveCsScriptActor(),
                new CommandCsScriptActor(),
                new CsScriptActor<CsScript>(),
            };
        }

        public T Load<T>(params IBindingValue[] bindings) where T : CsScript
        {
            var script = activator.Activate<T>(bindings);

            if (!actors.Any(c => c.Accept(script)))
                throw new InvalidOperationException($"no actor accepts scripts of type {typeof(T).FullName}");

            return script;
        }

        public override void Update(IGameEngineTime time)
        {
            foreach (var actor in actors)
                actor.Update(time);
        }
    }
}