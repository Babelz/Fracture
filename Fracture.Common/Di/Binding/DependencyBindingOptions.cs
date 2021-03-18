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
        /// Actual class of the dependency should be binded.
        /// </summary>
        Class = (1 << 1),

        /// <summary>
        /// All subclasses of the dependency should be binded.
        /// </summary>
        Classes = (1 << 2),

        /// <summary>
        /// All interfaces of the class and all it's sub classes should be binded.
        /// </summary>
        Interfaces = (1 << 3),

        /// <summary>
        /// All abstract classes and subclasses should be binded.
        /// </summary>
        AbstractClasses = (1 << 4),

        /// <summary>
        /// Causes options to work in strict manner. For example when using class option, all interfaces, abstract classes
        /// and subclasses can be mapped back to the actual dependency. Using strict prevents this kind of behaviour.
        /// </summary>
        Strict = (1 << 5),

        /// <summary>
        /// Combination to bind all abstract classes and interfaces.
        /// </summary>
        AbstractInterface = AbstractClasses | Interfaces,

        /// <summary>
        /// Combination to bind the base class and interfaces.
        /// </summary>
        ClassInterfaces = Class | Interfaces,

        /// <summary>
        /// Combination to bind classes and interfaces.
        /// </summary>
        ClassesInterfaces = Classes | Interfaces
    }
}
