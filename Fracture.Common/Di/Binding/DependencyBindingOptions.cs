using System;

namespace Fracture.Common.Di.Binding
{
    /// <summary>
    /// Enumeration that defines what data about the object should be associated for the binding.
    /// </summary>
    [Flags]
    public enum DependencyBindingOptions : byte
    {
        /// <summary>
        /// No binding options are given, should be handled as error.
        /// </summary>
        None = 0,

        /// <summary>
        /// Actual base type of the dependency should be bound.
        /// </summary>
        BaseType = 1 << 1,

        /// <summary>
        /// All sub types of the dependency should be bound.
        /// </summary>
        SubTypes = 1 << 2,

        /// <summary>
        /// All interfaces of the class and all its sub classes should be bound.
        /// </summary>
        Interfaces = 1 << 3,

        /// <summary>
        /// All abstract classes and subclasses should be bound.
        /// </summary>
        Abstracts = 1 << 4,

        /// <summary>
        /// Causes options to work in strict manner. For example when using class option, all interfaces, abstract classes
        /// and subclasses can be mapped back to the actual dependency. Using strict prevents this kind of behaviour.
        /// </summary>
        Strict = 1 << 5,
    }
}