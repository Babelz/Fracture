using System;

namespace Fracture.Common.Di.Attributes
{
    /// <summary>
    /// Attribute that marks a property to be used for initialization with dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class BindingPropertyAttribute : Attribute
    {
        // Marker attribute. No members.
    }
}
