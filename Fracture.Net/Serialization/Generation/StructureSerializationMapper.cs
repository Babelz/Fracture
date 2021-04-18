using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Enumeration defining serialization path for manually mapped value.
    /// </summary>
    public enum ValueSerializationPath : byte
    {
        Property = 0,
        Field
    }
    
    /// <summary>
    /// Structure that contains hint how single value path should be serialized from a structure or a class.
    /// </summary>
    public readonly struct ValueSerializationMappingHint
    {
        #region Properties
        /// <summary>
        /// Gets the name of the field or property.
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the path of the mapping.
        /// </summary>
        public ValueSerializationPath Path
        {
            get;
        }
        #endregion

        public ValueSerializationMappingHint(string name, ValueSerializationPath path)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Path = path;
        }
    }
    
    /// <summary>
    /// Structure containing value mapping schematics for single field or property of an structure or a class.
    /// </summary>
    public readonly struct ValueSerializationMapping
    {
        #region Properties
        /// <summary>
        /// Gets the reflected field of this mapping.
        /// </summary>
        public FieldInfo Field
        {
            get;
        }

        /// <summary>
        /// Gets the reflected property of this mapping.
        /// </summary>
        public PropertyInfo Property
        {
            get;
        }
        
        /// <summary>
        /// Gets the property or field name of the mapping.
        /// </summary>
        public string Name => IsField ? Field.Name : Property.Name;

        /// <summary>
        /// Returns boolean declaring whether this mapping points to a field.
        /// </summary>
        public bool IsField => Field != null;
        
        /// <summary>
        /// Returns boolean declaring whether this mapping points to a property.
        /// </summary>
        public bool IsProperty => Property != null;
        #endregion

        public ValueSerializationMapping(FieldInfo field = null, PropertyInfo property = null)
        {
            Field    = field;
            Property = property;
            
            // Require at least one path to have value.
            if (property == null && field == null)
                throw new ArgumentNullException($"one of {nameof(field)} or {nameof(property)} is required to have value");
            
            // Make sure only one path has value.
            if (property != null && field != null)
                throw new InvalidOperationException($"expecting only one of {nameof(field)} or {nameof(property)} to have value");
        }
    }
    
    /// <summary>
    /// Class containing all mapping semantics regarding single structure or class.
    /// </summary>
    public sealed class StructureSerializationMapping
    {
        #region Properties
        /// <summary>
        /// Gets the type that is being mapped.
        /// </summary>
        public Type Type
        {
            get;
        }
        
        /// <summary>
        /// Gets the value mappings associated with this mapping.
        /// </summary>
        public IReadOnlyCollection<ValueSerializationMapping> ValueMappings
        {
            get;
        }
        #endregion

        public StructureSerializationMapping(Type type, IReadOnlyCollection<ValueSerializationMapping> valueMappings)
        {
            Type          = type;
            ValueMappings = valueMappings;
        }
    }
    
    /// <summary>
    /// Static utility class for mapping structures to their properties.
    ///
    /// Structure serialization has the following constraints in place:
    ///    - Types with public parameterless constructors are only supported
    ///    - Fields are first in order for the serialization
    ///    - Properties are serialized after fields
    ///
    /// It is considered a good practice to only use properties or fields in your objects. Fields and properties
    /// are both being supported by serialization as third party types used in serialized objects might use
    /// both properties and fields.
    /// </summary>
    public static class StructureSerializationMapper
    {
        /// <summary>
        /// Automatically maps given type to <see cref="StructureSerializationMapping"/> that can be used as
        /// instructions for serializing the structure dynamically.
        /// </summary>
        public static StructureSerializationMapping Map(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                             .Where(f => !f.IsInitOnly)
                             .Select(f => new ValueSerializationMapping(field: f));
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.CanWrite)
                                 .Select(p => new ValueSerializationMapping(property: p));
            
            return new StructureSerializationMapping(type, fields.Concat(properties).ToArray());
        }
        
        /// <summary>
        /// Maps manually given type to <see cref="StructureSerializationMapping"/> that can be used as instructions
        /// for serializing the structure dynamically. Generation is based on given hints and only fields
        /// and properties that have hints are included.
        /// </summary>
        public static StructureSerializationMapping Map(Type type, params ValueSerializationMappingHint[] hints)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .Select(f => new ValueSerializationMapping(field: f));
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Select(p => new ValueSerializationMapping(property: p));
            
            var mappings = fields.Concat(properties).Where((m) =>
            {
                var hint = hints.FirstOrDefault(h => h.Name == m.Name);
                
                if (hint.Equals(default(ValueSerializationMappingHint)))
                    return false;
                
                return (m.IsField && hint.Path == ValueSerializationPath.Field) ||
                       (m.IsProperty && hint.Path != ValueSerializationPath.Property);
            }).ToArray();
            
            return new StructureSerializationMapping(type, mappings);
        }
    }
}