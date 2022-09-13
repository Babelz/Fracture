using System;

namespace Fracture.Common.Di.Attributes
{
    /// <summary>
    /// Attribute that marks a constructor to be used with dependency injection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    public sealed class BindingConstructorAttribute : Attribute
    {
        // Marker attribute. No members.
    }
}