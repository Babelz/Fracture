﻿using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Interface for implementing classes that activate dependencies.
    /// </summary>
    public interface IDependencyActivator
    {
        /// <summary>
        /// Attempts to active dependency of given type and returns it to the caller via out instance. Throws if the dependency could not be activated.
        /// </summary>
        void Activate(Type type, out object instance);
    }

    /// <summary>
    /// Activator that activates objects using default constructor.
    /// </summary>
    public sealed class DependencyDefaultConstructorActivator : IDependencyActivator
    {
        public DependencyDefaultConstructorActivator()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasDefaultConstructor(Type type)
        {
            if (!DependencyTypeMapper.HasDefaultConstructor(type))
                throw new DependencyBinderException(type, "type does not have default constructor");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InternalActivate(Type type, out object instance)
        {
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new DependencyBinderException(type,
                                                    $"could not create instance of {type.Name} using default " +
                                                    $"parameterless constructor, please check that the type has a " +
                                                    $"public parameterless default constructor available",
                                                    e);
            }

            if (instance == null)
                throw new DependencyBinderException(type, "could not activate dependency");
        }

        public void Activate(Type type, out object instance)
        {
            AssertTypeHasDefaultConstructor(type);

            InternalActivate(type, out instance);
        }
    }

    /// <summary>
    /// Activator that activates objects using binding constructor.
    /// </summary>
    public sealed class DependencyBindingConstructorActivator : IDependencyActivator
    {
        #region Fields
        private readonly DependencyBindingValueLocator locator;
        #endregion

        public DependencyBindingConstructorActivator(DependencyBindingValueLocator locator)
            => this.locator = locator ?? throw new ArgumentNullException(nameof(locator));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertTypeHasBindingConstructor(Type type)
        {
            if (!DependencyTypeMapper.HasBindingConstructor(type))
            {
                throw new DependencyBinderException(type,
                                                    $"type {type.Name} does not contain constructor annotated with " +
                                                    $"{nameof(BindingConstructorAttribute)}");
            }
        }

        private void InternalActivate(Type type, out object instance)
        {
            instance = null;

            // Check that all arguments can be located.
            var constructor = DependencyTypeMapper.GetBindingConstructor(type);

            if (!locator.BindingsExist(constructor))
                throw new DependencyBinderException(type, "unable to bind to constructor, missing binding values");

            // Get all arguments and create the instance.
            instance = constructor.Invoke(locator.GetConstructorBindingValues(constructor));

            if (instance == null)
                throw new DependencyBinderException(type, "could not activate dependency");
        }

        public void Activate(Type type, out object instance)
        {
            AssertTypeHasBindingConstructor(type);

            InternalActivate(type, out instance);
        }
    }
}