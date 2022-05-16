using System;

namespace Fracture.Common.Di.Attributes
{
    /// <summary>
    /// Attribute that marks a method to be used for initialization with dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class BindingMethodAttribute : Attribute
    {
        // Marker attribute. No members.
    }
}