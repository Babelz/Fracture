using System;
using System.Collections.Generic;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Class that handles dependency and binding resolving.
    /// </summary>
    public sealed class DependencyBindingResolver
    {
        #region Fields
        private readonly IDependencyLocator locator;
        #endregion

        public DependencyBindingResolver(IDependencyLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator));

        public bool ResolveActivator(Type type, IBindingValue[] values, out IDependencyActivator activator)
        {
            if (type.IsAbstract)
                throw new DependencyBinderException(type, $"can't create instance of abstract type {type.Name}");

            if (type.IsInterface)
                throw new DependencyBinderException(type, $"can't create instance of interface type {type.Name}");

            if (DependencyTypeMapper.HasBindingConstructor(type))
                activator = new DependencyBindingConstructorActivator(new DependencyBindingValueLocator(locator, values));
            else if (DependencyTypeMapper.HasDefaultConstructor(type))
                activator = new DependencyDefaultConstructorActivator();
            else
                activator = null;

            return activator != null;
        }

        public bool ResolveBindings(Type type, IBindingValue[] values, out List<IDependencyBinding> bindings)
        {
            bindings = new List<IDependencyBinding>();

            if (DependencyTypeMapper.HasBindingProperties(type))
                bindings.Add(new DependencyPropertyBinding(new DependencyBindingValueLocator(locator, values)));

            if (DependencyTypeMapper.HasBindingMethods(type))
                bindings.Add(new DependencyMethodBinding(new DependencyBindingValueLocator(locator, values)));

            return bindings.Count != 0;
        }
    }
}