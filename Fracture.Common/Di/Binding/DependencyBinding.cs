using System;
using System.Linq;
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
        private readonly DependencyBindingValueLocator locator;
        #endregion

        public DependencyMethodBinding(DependencyBindingValueLocator locator)
        {
            this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasBindingMethods(Type type)
        {
            if (!DependencyTypeMapper.HasBindingMethods(type))
                throw new DependencyBinderException(type,
                                                    $"type {type.Name} does not contain any methods " +
                                                    $"annotated with {nameof(BindingMethodAttribute)}");
        }

        private void InternalBind(object instance)
        {
            var methods = DependencyTypeMapper.GetBindingMethods(instance.GetType()).ToArray();

            if (!methods.All(locator.BindingsExist))
                throw new DependencyBinderException(instance.GetType(), "unable to bind to method, missing binding values");

            foreach (var method in methods)
                method.Invoke(instance, locator.GetMethodBindingValues(method));
        }

        public void Bind(object instance)
        {
            AssertTypeHasBindingMethods(instance.GetType());

            InternalBind(instance);
        }
    }

    /// <summary>
    /// Binding that passes dependencies to properties of and object.
    /// </summary>
    public sealed class DependencyPropertyBinding : IDependencyBinding
    {
        #region Fields
        private readonly DependencyBindingValueLocator locator;
        #endregion

        public DependencyPropertyBinding(DependencyBindingValueLocator locator)
        {
            this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasBindingProperties(Type type)
        {
            if (!DependencyTypeMapper.HasBindingProperties(type))
                throw new DependencyBinderException(type,
                                                    $"type {type.Name} does not contain any properties " +
                                                    $"annotated with {nameof(BindingPropertyAttribute)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateBindingProperties(Type type)
        {
            if (DependencyTypeMapper.GetBindingProperties(type).Any(p => !p.CanWrite))
                throw new InvalidOperationException($"one or more binding property on type {type.Name} is read only");
        }

        private void InternalBind(object instance)
        {
            var properties = DependencyTypeMapper.GetBindingProperties(instance.GetType()).ToArray();

            if (!properties.All(locator.BindingExist))
                throw new DependencyBinderException(instance.GetType(), "unable to bind to property, missing binding value");

            foreach (var property in properties)
                property.SetValue(instance, locator.GetPropertyBindingValue(property));
        }

        public void Bind(object instance)
        {
            AssertTypeHasBindingProperties(instance.GetType());

            ValidateBindingProperties(instance.GetType());

            InternalBind(instance);
        }
    }
}