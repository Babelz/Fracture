using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Interface for implementing binding values that are used for binding when the dependency is being constructed. Binding values are selected for activation
    /// based on their names and not by their types. 
    /// </summary>
    public interface IBindingValue
    {
        #region Properties
        /// <summary>
        /// Gets the name of the binding. This can point to parameter, property or field names.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the value of the binding.
        /// </summary>
        object Value
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Structure defining constant binding value.
    /// </summary>
    public readonly struct ConstantBinding : IBindingValue
    {
        #region Properties
        public string Name
        {
            get;
        }

        public object Value
        {
            get;
        }
        #endregion

        public ConstantBinding(string name, object value)
        {
            Name  = name;
            Value = value;
        }
    }

    /// <summary>
    /// Binding defining indirect varying binding value.
    /// </summary>
    public readonly struct VariableBinding : IBindingValue
    {
        #region Fields
        private readonly Func<object> locator;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }

        public object Value => locator();
        #endregion

        public VariableBinding(string name, Func<object> locator)
        {
            Name = name;

            this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
        }
    }

    public static class BindingValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IBindingValue Var(string name, Func<object> locator)
            => new VariableBinding(name, locator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IBindingValue Const(string name, object value)
            => new ConstantBinding(name, value);
    }

    public class DependencyBindingValueLocator
    {
        #region Fields
        private readonly IDependencyLocator locator;
        private readonly IBindingValue[]    values;
        #endregion

        public DependencyBindingValueLocator(IDependencyLocator locator, IBindingValue[] values)
        {
            this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
            this.values  = values ?? Array.Empty<IBindingValue>();
        }

        private bool ParameterBindingsExist(IEnumerable<ParameterInfo> parameters)
            => parameters.All(p => values.Any(v => v.Name == p.Name) || locator.Exists(p.ParameterType) || p.HasDefaultValue);

        private object[] GetParameterBindingValues(IEnumerable<ParameterInfo> parameters)
            => parameters.Select(p => values.FirstOrDefault(v => v.Name == p.Name)?.Value ??
                                      (locator.Exists(p.ParameterType) ? locator.First(p.ParameterType) : p.DefaultValue))
                .ToArray();

        public bool BindingExist(PropertyInfo property)
            => values.Any(v => v.Name == property.Name) || locator.Exists(property.PropertyType);

        public bool BindingsExist(ConstructorInfo constructor)
            => ParameterBindingsExist(constructor.GetParameters());

        public bool BindingsExist(MethodInfo method)
            => ParameterBindingsExist(method.GetParameters());

        public object GetPropertyBindingValue(PropertyInfo property)
            => values.FirstOrDefault(v => v.Name == property.Name)?.Value ?? locator.First(property.PropertyType);

        public object[] GetConstructorBindingValues(ConstructorInfo constructor)
            => GetParameterBindingValues(constructor.GetParameters());

        public object[] GetMethodBindingValues(MethodInfo method)
            => GetParameterBindingValues(method.GetParameters());
    }
}