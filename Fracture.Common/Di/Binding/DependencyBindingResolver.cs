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
        
        public bool ResolveActivator(Type type, out IDependencyActivator activator)
        {
            if (type.IsAbstract)
                throw new DependencyBinderException(type, $"can't create instance of abstract type {type.Name}");
            
            if (type.IsInterface)
                throw new DependencyBinderException(type, $"can't create instance of interface type {type.Name}");
            
            if (DependencyTypeMapper.HasBindingConstructor(type)) 
                activator = new DependencyBindingConstructorActivator(locator);
            else if (DependencyTypeMapper.HasDefaultConstructor(type)) 
                activator = new DependencyDefaultConstructorActivator();
            else                                                           
                activator = null;

            return activator != null;
        }

        public bool ResolveBindings(Type type, out List<IDependencyBinding> bindings)
        {
            bindings = new List<IDependencyBinding>();

            if (DependencyTypeMapper.HasBindingProperties(type)) 
                bindings.Add(new DependencyPropertyBinding(locator));
            
            if (DependencyTypeMapper.HasBindingMethods(type))    
                bindings.Add(new DependencyMethodBinding(locator));

            return bindings.Count != 0;
        }
    }
}
