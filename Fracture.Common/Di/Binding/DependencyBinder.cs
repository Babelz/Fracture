using System;
using System.Collections.Generic;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Class that handles dependency binding to object.
    /// </summary>
    public sealed class DependencyBinder
    {
        #region Fields
        private IDependencyActivator activator;

        private readonly List<IDependencyBinding> binders;

        private object instance;
        #endregion

        #region Properties
        public object Instance => instance;

        public Type Proxy
        {
            get;
            private set;
        }

        public Type Type
        {
            get;
        }

        public DependencyBindingOptions Options
        {
            get;
        }
        #endregion

        public DependencyBinder(DependencyBindingOptions options, object instance)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));

            Options = options;
            Type    = instance.GetType();

            binders = new List<IDependencyBinding>();
        }

        public DependencyBinder(DependencyBindingOptions options, Type type)
        {
            Options = options;

            Type = type ?? throw new ArgumentNullException(nameof(type));

            binders = new List<IDependencyBinding>();
        }

        public void BindWith(IDependencyActivator activator)
        {
            if (instance != null)
                throw new InvalidOperationException("already instantiated");

            this.activator = activator ?? throw new ArgumentNullException(nameof(activator));
        }

        public void BindWith(IEnumerable<IDependencyBinding> bindings)
            => binders.AddRange(bindings);

        public void AsProxy(Type proxy)
        {
            Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));

            var actual = instance?.GetType() ?? Type;

            if (!proxy.IsAssignableFrom(actual))
                throw new InvalidOperationException("invalid proxy type");
        }

        public void Bind()
        {
            if (activator != null && instance == null)
                activator.Activate(Type, out instance);

            for (var i = binders.Count - 1; i >= 0; i--)
                binders[i].Bind(instance);
        }
    }
}