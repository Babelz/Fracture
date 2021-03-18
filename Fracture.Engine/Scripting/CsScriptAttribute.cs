using System;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Attribute for marking scripts that should not be loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CsScriptIgnoreAttribute : Attribute
    {
        public CsScriptIgnoreAttribute()
        {
        }
    }
    
    /// <summary>
    /// Attribute for marking script definitions (base classes).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CsScriptDefinitionAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// In case of reusable scripts, this contains
        /// the allocation size.
        /// </summary>
        public int Allocations
        {
            get;
        }
        
        /// <summary>
        /// Are these scripts reusable by design.
        /// </summary>
        public bool Reusable
            => Allocations != 0;

        /// <summary>
        /// Are these scripts unique, meaning no
        /// more than one instance of them exist.
        /// </summary>
        public bool Unique
        {
            get;
        }
        #endregion

        public CsScriptDefinitionAttribute()
        {
        }

        public CsScriptDefinitionAttribute(bool unique)
        {
            Unique = unique;
        }

        public CsScriptDefinitionAttribute(int allocations)
        {
            Allocations = allocations;
        }
    }
    
    /// <summary>
    /// Attribute for marking classes that are used for scripting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CsScriptAttribute : Attribute
    {
        #region Properties
        /// <summary>
        /// Defines the name of the script.
        /// </summary>
        public string Name
        {
            get;
        }
        #endregion

        public CsScriptAttribute(string name)
            => Name = name;
    }
}
