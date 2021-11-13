using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Binding;

namespace Fracture.Common.Di
{
    /// <summary>
    /// Interface for implementing dependency locators. Locators are used for locating dependencies. These dependencies 
    /// are bound by the binder.
    /// </summary>
    public interface IDependencyLocator
    {
        IEnumerable<object> All(Func<object, bool> predicate);
        IEnumerable<object> All();
        
        IEnumerable<T> All<T>(Func<T, bool> predicate);
        IEnumerable<T> All<T>();

        object First(Type type, Func<object, bool> predicate);
        object First(Type type);

        T First<T>(Func<T, bool> predicate);
        T First<T>();

        bool Exists(Type type, Func<object, bool> predicate);
        bool Exists(Type type);

        bool Exists<T>(Func<T, bool> predicate);
        bool Exists<T>();
    }

    /// <summary>
    /// Interface for implementing dependency binders. Binders provide dependency binding in various ways. Also
    /// supports proxy bindings.
    /// </summary>
    public interface IDependencyBinder
    {
        void Unbind(Type type);
        void Unbind(object instance);
        void Unbind(object instance, Type proxy);
        void Unbind(Type type, Type proxy);

        void Bind(Type type, params IBindingValue[] values);
        void Bind<T>(params IBindingValue[] bindings);
        void Bind(object instance, params IBindingValue[] values);

        void Proxy(Type actual, Type proxy, params IBindingValue[] values);
        void Proxy<T>(Type proxy, params IBindingValue[] values);
        void Proxy(object instance, Type proxy, params IBindingValue[] values);
        void Proxy<T>(object instance, params IBindingValue[] values);
    }
    
    /// <summary>
    /// Interface for implementing object generic object activators.
    /// </summary>
    public interface IObjectActivator
    {
        /// <summary>
        /// Attempts to activate object of specified type. Dependencies for this object are retrieved from the kernel. The object will not be registered to the
        /// kernel after it has been activated.
        /// </summary>
        object Activate(Type type, params IBindingValue[] values);
        
        /// <summary>
                                          
        /// Attempts to activate object of specified type. Dependencies for this object are retrieved from the kernel. The object will not be registered to the
        /// kernel after it has been activated.
        /// </summary>
        T Activate<T>(params IBindingValue[] values);
    }
    
    /// <summary>
    /// Class that provides simple dependency injection kernels. Kernels combine binding and locating
    /// dependencies under single interface.
    /// </summary>
    public class Kernel : IDependencyLocator, IDependencyBinder, IObjectActivator
    {
        #region Fields
        private readonly DependencyBindingOptions bindingOptions;
        private readonly DependencyBindingOptions proxyOptions;
        
        private readonly List<DependencyBinder> binders;
        private readonly List<Dependency> dependencies;
        #endregion
        
        public Kernel(DependencyBindingOptions bindingOptions = DependencyBindingOptions.ClassesInterfaces, 
                      DependencyBindingOptions proxyOptions = DependencyBindingOptions.ClassesInterfaces)
        {
            this.bindingOptions = bindingOptions;
            this.proxyOptions   = proxyOptions;
            
            binders      = new List<DependencyBinder>();
            dependencies = new List<Dependency>();
        }
        
        public Kernel(DependencyBindingOptions bindingOptions)
            : this(bindingOptions, bindingOptions)
        {
        }
        
        private static bool ConstructDependency(DependencyBinder binder, out Dependency dependency)
        {
            dependency = null;

            if (!binder.TryBind()) return false;

            var options = binder.Options;
            var strict  = (options & DependencyBindingOptions.Strict) == DependencyBindingOptions.Strict;

            dependency = binder.Proxy != null ? 
                new Dependency(binder.Instance, DependencyTypeMapper.Map(binder.Proxy, options), strict) : 
                new Dependency(binder.Instance, DependencyTypeMapper.Map(binder.Type, options), strict);

            return true;
        }

        private DependencyBinder ConstructBinder(Type type, Type proxy, object instance, DependencyBindingOptions options, IBindingValue[] values)
        {
            if (type != null && instance != null)
                throw new InvalidOperationException("both type and instance can't have a value");

            DependencyBinder binder;
            
            if      (instance != null) binder = new DependencyBinder(options, instance);
            else if (type != null)     binder = new DependencyBinder(options, type);
            else                       throw new InvalidOperationException("construction requires type of instance to continue");
            
            var resolver = new DependencyBindingResolver(this);

            if (proxy != null) binder.AsProxy(proxy);

            if (instance == null)
            {
                if (!resolver.ResolveActivator(type, values, out var activator))
                {
                    throw new DependencyBinderException(type,
                                                        $"no activator for type {type.Name} could be created, please " +
                                                        $"check that the type has a public parameterless default " +
                                                        $"constructor or binding constructor available");
                }
                
                binder.BindWith(activator);
            }

            if (!resolver.ResolveBindings(type ?? instance?.GetType(), values, out var bindings)) 
                return binder;
            
            binder.BindWith(bindings);

            return binder;
        }

        private void UpdateBinders()
        {
            var i = 0;

            while (i < binders.Count)
            {
                if (ConstructDependency(binders[i], out var dependency))
                {
                    binders.RemoveAt(i);

                    dependencies.Add(dependency);

                    continue;
                }

                i++;
            }
        }

        public object Activate(Type type, params IBindingValue[] values)
        {
            var binder = ConstructBinder(type, null, null, DependencyBindingOptions.Class, values);
            
            binder.Bind();
            
            return binder.Instance;
        }
        
        public T Activate<T>(params IBindingValue[] values)
            => (T)Activate(typeof(T), values);

        public IEnumerable<object> All(Func<object, bool> predicate)
            => dependencies.Where(d => predicate(d.Cast<object>()));

        public IEnumerable<object> All()
            => dependencies.Select(d => d.Cast<object>());

        public IEnumerable<T> All<T>(Func<T, bool> predicate)
            => dependencies.Where(d => d.Castable<T>())
                           .Select(d => d.Cast<T>())
                           .Where(predicate);

        public IEnumerable<T> All<T>()
            => dependencies.Where(d => d.Castable<T>())
                           .Select(d => d.Cast<T>());

        public void Unbind(Type type)
        {
            var binder = binders.FirstOrDefault(b => b.Type == type);

            if (binder != null) 
                binders.Remove(binder);
            else
            {
                var dependency = dependencies.FirstOrDefault(d => d.Castable(type));

                if (dependency == null)
                    throw new InvalidOperationException($"dependency of type {type.Name} does not exist");

                dependencies.Remove(dependency);
            }
        }

        public void Unbind(object instance)
        {
            var dependency = dependencies.FirstOrDefault(d => d.ReferenceEquals(instance));

            if (dependency == null)
                throw new InvalidOperationException($"dependency of type {instance.GetType().Name} does not exist");

            dependencies.Remove(dependency);
        }

        public void Unbind(object instance, Type proxy)
        {
            var binder = binders.FirstOrDefault(b => b.Type == instance.GetType() &&
                                                     b.Proxy == proxy);

            if (binder != null) 
                binders.Remove(binder);
            else
            {
                var dependency = dependencies.FirstOrDefault(d => d.ReferenceEquals(instance) && d.Castable(proxy));

                if (dependency == null)
                    throw new InvalidOperationException($"dependency of type {instance.GetType().Name} does not exist");

                dependencies.Remove(dependency);
            }
        }
        public void Unbind(Type type, Type proxy)
        {
            var binder = binders.FirstOrDefault(b => b.Type == type && b.Proxy == proxy);

            if (binder != null)
                binders.Remove(binder);
            else
            {
                var dependency = dependencies.FirstOrDefault(d => d.Castable(type) && d.Castable(proxy));

                if (dependency == null)
                    throw new InvalidOperationException($"dependency of type {type.Name} does not exist");

                dependencies.Remove(dependency);
            }
        }

        public void Bind(Type type, params IBindingValue[] values)
        {
            var binder = ConstructBinder(type, null, null, bindingOptions, values);

            if (ConstructDependency(binder, out var dependency))
                dependencies.Add(dependency);
            else
                binders.Add(binder);

            UpdateBinders();
        }
        
        public void Bind<T>(params IBindingValue[] bindings)
            => Bind(typeof(T), bindings);

        public void Bind(object instance, params IBindingValue[] values)
        {
            var binder = ConstructBinder(null, null, instance, bindingOptions, values);

            if (ConstructDependency(binder, out var dependency))
                dependencies.Add(dependency);
            else
                binders.Add(binder);

            UpdateBinders();
        }

        public void Proxy(Type actual, Type proxy, params IBindingValue[] values)
        {
            var binder = ConstructBinder(actual, proxy, null, proxyOptions, values);

            if (ConstructDependency(binder, out var dependency))
                dependencies.Add(dependency);
            else
                binders.Add(binder);

            UpdateBinders();
        }

        public void Proxy<T>(Type proxy, params IBindingValue[] values)
            => Proxy(proxy, typeof(T), values);

        public void Proxy(object instance, Type proxy, params IBindingValue[] values)
        {
            var binder = ConstructBinder(null, proxy, instance, proxyOptions, values);

            if (ConstructDependency(binder, out var dependency))
                dependencies.Add(dependency);
            else
                binders.Add(binder);

            UpdateBinders();
        }

        public void Proxy<T>(object instance, params IBindingValue[] values)
            => Proxy(instance, typeof(T));

        public bool Exists(Type type, Func<object, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return dependencies.Any(d => d.Castable(type) && predicate(d.Cast<object>()));
        }

        public bool Exists(Type type)
            => dependencies.Any(d => d.Castable(type));
        
        public bool Exists<T>(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return dependencies.Any(d => d.Castable<T>() && predicate(d.Cast<T>()));
        }

        public bool Exists<T>()
            => dependencies.Any(d => d.Castable<T>());

        public object First(Type type, Func<object, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var dependency = dependencies.FirstOrDefault(d => d.Castable(type) && predicate(d.Cast<object>()));

            if (dependency == null)
                throw new DependencyNotFoundException(type);

            return dependency.Cast<object>();
        }

        public object First(Type type)
        {
            var dependency = dependencies.FirstOrDefault(d => d.Castable(type));

            if (dependency == null)
                throw new DependencyNotFoundException(type);

            return dependency.Cast<object>();
        }

        public T First<T>(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var dependency = dependencies.FirstOrDefault(d => d.Castable<T>() && predicate(d.Cast<T>()));

            if (dependency == null)
                throw new DependencyNotFoundException(typeof(T));

            return dependency.Cast<T>();
        }

        public T First<T>()
        {
            var dependency = dependencies.FirstOrDefault(d => d.Castable<T>());

            if (dependency == null)
                throw new DependencyNotFoundException(typeof(T));

            return dependency.Cast<T>();
        }
    }
}