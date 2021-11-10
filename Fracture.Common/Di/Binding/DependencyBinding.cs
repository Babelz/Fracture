using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Interface for implemented bindings. Bindings are used for late binding of dependencies
    /// </summary>
    public interface IDependencyBinding
    {
        /// <summary>
        /// Attempts to bind dependencies to given object.
        /// </summary>
        /// <returns>boolean declaring whether the binding was successful</returns>
        bool TryBind(object instance);
        
        /// <summary>
        /// Attempts to bind dependencies to given object. Throws if binding could not be complete.
        /// </summary>
        void Bind(object instance);
    }
    
    /// <summary>
    /// Dependency binding that binds to method bindings of objects.
    /// </summary>
    public sealed class DependencyMethodBinding : IDependencyBinding
    {
        #region Fields
        private readonly IDependencyLocator locator;
        #endregion

        public DependencyMethodBinding(IDependencyLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasBindingMethods(Type type)
        {
            if (!DependencyTypeMapper.HasBindingMethods(type))
                throw new DependencyBinderException(type, 
                                                    $"type {type.Name} does not contain any methods " +
                                                    $"annotated with {nameof(BindingMethodAttribute)}");
        }
        
        private bool CanBindToMethod(MethodInfo method)
            => method.GetParameters().All(t => locator.Exists(t.ParameterType));
        
        private void BindToMethod(object instance, MethodInfo method)
        {
            var parameters = method.GetParameters();
            var arguments  = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                arguments[i] = locator.First(parameters[i].ParameterType);

            method.Invoke(instance, arguments);
        }
        
        private void InternalBind(object instance)
        {
            var methods = instance.GetType()
                                  .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                  .Where(c => c.GetCustomAttribute<BindingMethodAttribute>() != null)
                                  .ToArray();

            if (!methods.All(CanBindToMethod))
                throw new DependencyBinderException(instance.GetType(), "unable to bind to method");
            
            for (var i = 0; i < methods.Length; i++)
                BindToMethod(instance, methods[i]);
        }
        
        public void Bind(object instance)
        {
            AssertTypeHasBindingMethods(instance.GetType());
            
            InternalBind(instance);
        }
        
        public bool TryBind(object instance)
        {
            AssertTypeHasBindingMethods(instance.GetType());
            
            try
            {
                InternalBind(instance);
            }
            catch (Exception)
            {
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Binding that passes dependencies to properties of and object.
    /// </summary>
    public sealed class DependencyPropertyBinding : IDependencyBinding
    {
        #region Fields
        private readonly IDependencyLocator locator;
        #endregion

        public DependencyPropertyBinding(IDependencyLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasBindingProperties(Type type)
        {
            if (!DependencyTypeMapper.HasBindingProperties(type))
                throw new DependencyBinderException(type,
                                                    $"type {type.Name} does not contain any properties " +
                                                    $"annotated with {nameof(BindingPropertyAttribute)}");
        }
        
        private void InternalBind(object instance)
        {
            var properties = instance.GetType()
                                     .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                     .Where(c => c.GetCustomAttribute<BindingPropertyAttribute>() != null)
                                     .ToArray();

            var arguments = new object[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                if (!locator.Exists(properties[i].PropertyType))
                    throw new DependencyBinderException(instance.GetType(), "binding does not exist");

                arguments[i] = locator.First(properties[i].PropertyType);
            }

            for (var i = 0; i < properties.Length; i++)
                properties[i].SetValue(instance, arguments[i]);
        }
        
        public void Bind(object instance)
        {    
            AssertTypeHasBindingProperties(instance.GetType());
            
            InternalBind(instance);
        }
        
        public bool TryBind(object instance)
        {
            AssertTypeHasBindingProperties(instance.GetType());
            
            try
            {
                InternalBind(instance);
            }
            catch (Exception)
            {
                return false;
            }
            
            return true;
        }
    }
}
