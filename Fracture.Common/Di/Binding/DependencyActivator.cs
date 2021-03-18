using System;
using System.Linq;
using System.Reflection;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Interface for implementing classes that activate dependencies.
    /// </summary>
    public interface IDependencyActivator
    {
        /// <summary>
        /// Attempts to active dependency of given type and returns 
        /// it to the caller via out instance.
        /// </summary>
        /// <returns>boolean declaring whether the type could be activated</returns>
        bool Activate(Type type, out object instance);
    }
    
    /// <summary>
    /// Activator that activates objects using default constructor.
    /// </summary>
    public sealed class DependencyDefaultConstructorActivator : IDependencyActivator
    {
        public DependencyDefaultConstructorActivator()
        {
        }
        
        public bool Activate(Type type, out object instance)
        {
            if (!DependencyTypeMapper.HasDefaultConstructor(type))
                throw new DependencyBinderException(type, "type does not have default constructor");

            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new DependencyBinderException(type,
                                                    $"could not create instance of {type.Name} using default " +
                                                    $"parameterless constructor, please check that the type has a " +
                                                    $"public parameterless default constructor available", e);
            }

            return true;
        }
    }
    
    /// <summary>
    /// Activator that activates objects using binding constructor.
    /// </summary>
    public sealed class DependencyBindingConstructorActivator : IDependencyActivator
    {
        #region Fields
        private readonly IDependencyLocator locator;
        #endregion

        public DependencyBindingConstructorActivator(IDependencyLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator));

        public bool Activate(Type type, out object instance)
        {
            if (!DependencyTypeMapper.HasBindingConstructor(type))
                throw new DependencyBinderException(type,
                                                    $"type {type.Name} does not contain constructor annotated with " +
                                                    $"{nameof(BindingConstructorAttribute)}");

            instance = null;

            foreach (var constructor in type.GetConstructors())
            {
                // Not a binding constructor, continue.
                if (constructor.GetCustomAttribute<BindingConstructorAttribute>() == null)
                    continue;
                
                // Check that all arguments can be located.
                var parameters = constructor.GetParameters();
                
                if (!parameters.All(p => locator.Exists(p.ParameterType)))
                    continue;
                
                // Get all arguments and create the instance.
                var arguments = parameters.Select(p => locator.First(p.ParameterType))
                                          .ToArray();
                
                instance = constructor.Invoke(arguments);

                return true;
            }
            
            return false;
        }
    }
}
