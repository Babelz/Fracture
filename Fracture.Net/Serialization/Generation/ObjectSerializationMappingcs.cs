using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;

namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Enumeration defining serialization path for values of the object.
    /// </summary>
    public enum SerializationValueLocation : byte
    {
        /// <summary>
        /// Path points to a public property.
        /// </summary>
        Property = 0,
        
        /// <summary>
        /// Path points to a public field.
        /// </summary>
        Field
    }
    
    /// <summary>
    /// Enumeration how structures are activated when they are being deserialized.
    /// </summary>
    public enum ObjectActivationMethod : byte
    {
        /// <summary>
        /// Object initialization happens using the default constructor.
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Object initialization happens using specified parametrized constructor.
        /// </summary>
        Parametrized
    }

    /// <summary>
    /// Structure that contains hint how single value path from constructor should be serialized from a structure
    /// or a class.
    /// </summary>
    public readonly struct ObjectActivationHint
    {
        #region Properties
        /// <summary>
        /// Gets the argument name in the constructor.
        /// </summary>
        public string ArgumentName
        {
            get;
        }

        /// <summary>
        /// Gets the value that this argument is pointing to.
        /// </summary>
        public SerializationValueHint Value
        {
            get;
        }
        #endregion

        public ObjectActivationHint(string argumentName, SerializationValueHint value)
        {
            ArgumentName = !string.IsNullOrEmpty(argumentName) ? argumentName : throw new ArgumentNullException(nameof(argumentName));
            Value        = value;
        }
    }
    
    /// <summary>
    /// Structure that contains hint how single value path should be serialized from a structure or a class.
    /// </summary>
    public readonly struct SerializationValueHint
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
        public SerializationValueLocation Path
        {
            get;
        }
        #endregion

        public SerializationValueHint(string name, SerializationValueLocation path)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Path = path;
        }
    }
    
    /// <summary>
    /// Structure containing value mapping schematics for single field or property of an structure or a class.
    /// </summary>
    public readonly struct SerializationValue
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

        public SerializationValue(FieldInfo field = null, PropertyInfo property = null)
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
    /// Structure containing object activation schematics of an structure or a class for serialization. 
    /// </summary>
    public readonly struct ObjectActivator
    {
        #region Properties
        /// <summary>
        /// Gets the constructor that should be used for object activation.
        /// </summary>
        public ConstructorInfo Constructor
        {
            get;
        }
        
        /// <summary>
        /// Gets the optional parameter list if non-default constructor is being used for activation.
        /// </summary>
        public IReadOnlyCollection<ObjectActivationHint> Parameters
        {
            get;
        }
        
        /// <summary>
        /// Gets boolean declaring whether this object activator is using the default constructor.
        /// </summary>
        public bool IsDefaultConstructor => Parameters == null || Parameters.Count == 0;
        #endregion

        public ObjectActivator(ConstructorInfo constructor, IReadOnlyCollection<ObjectActivationHint> parameters)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Parameters  = parameters;
        }
    }
    
    /// <summary>
    /// Class containing all mapping semantics regarding single structure or a class.
    /// </summary>
    public sealed class ObjectSerializationMapping
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
        public IReadOnlyCollection<SerializationValue> Values
        {
            get;
        }
        
        /// <summary>
        /// Gets the activator for this serialization mapping.
        /// </summary>
        public ObjectActivator Activator
        {
            get;
        }
        #endregion

        public ObjectSerializationMapping(Type type, 
                                          IReadOnlyCollection<SerializationValue> values,
                                          ObjectActivator activator)
        {
            Type      = type ?? throw new ArgumentNullException(nameof(type));
            Values    = values ?? throw new ArgumentNullException(nameof(activator));
            Activator = activator;
        }
    }
    
    /// <summary>
    /// Interface for creating object serialization mappers. Mappers provide builder interface for mapping
    /// objects for serialization.
    /// </summary>
    public interface IObjectSerializationMapper
    {
        /// <summary>
        /// Directs the builder that given type is being mapped.
        /// </summary>
        IObjectSerializationMapper WithType(Type type);

        /// <summary>
        /// Directs the builder that given type is being mapped.
        /// </summary>
        IObjectSerializationMapper WithType<T>();

        /// <summary>
        /// Directs the builder to map the types default constructor for activation.
        /// </summary>
        IObjectSerializationMapper WithDefaultConstructor();
        
        /// <summary>
        /// Directs the builder to map the types constructor that matches given hints.
        /// </summary>
        IObjectSerializationMapper WithConstructor(params ObjectActivationHint[] hints);
        
        /// <summary>
        /// Directs the builder to map types all public properties.
        /// </summary>
        /// <param name="filtered">optional list of properties that will be filtered</param>
        IObjectSerializationMapper WithPublicProperties(params string[] filtered);
        
        /// <summary>
        /// Directs the builder to map types all public fields.
        /// </summary>
        /// <param name="filtered">optional list of fields that will be filtered</param>
        IObjectSerializationMapper WithPublicFields(params string[] filtered);
        
        /// <summary>
        /// Directs the builder to map types all public fields and properties based on given hints.
        /// </summary>
        IObjectSerializationMapper WithValues(params SerializationValueHint[] values);
        
        /// <summary>
        /// Creates the mapping and returns in to the caller.
        /// </summary>
        ObjectSerializationMapping Map();
    }

    /// <summary>
    /// Default implementation of <see cref="IObjectSerializationMapper"/>. 
    /// </summary>
    public sealed class ObjectSerializationMapper : IObjectSerializationMapper
    {
        
    }
    
    // /// <summary>
    // /// Static utility class for mapping structures to their properties.
    // /// </summary>
    // public static class ObjectSerializationMapper
    // {
    //     /// <summary>
    //     /// Automatically maps given type to <see cref="StructureSerializationMapping"/> that can be used as
    //     /// instructions for serializing the structure dynamically.
    //     /// </summary>
    //     public static StructureSerializationMapping AutoMap(Type type)
    //     {
    //         var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
    //                          .Where(f => !f.IsInitOnly)
    //                          .Select(f => new SerializationValue(field: f));
    //         
    //         var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //                              .Where(p => p.CanRead && p.CanWrite)
    //                              .Select(p => new SerializationValue(property: p));
    //         
    //         return new StructureSerializationMapping(type, fields.Concat(properties).ToArray());
    //     }
    //     
    //     public static StructureSerializationMapping ConstructorMap(Type type, 
    //     
    //     /// <summary>
    //     /// Maps manually given type to <see cref="StructureSerializationMapping"/> that can be used as instructions
    //     /// for serializing the structure dynamically. Generation is based on given hints and only fields
    //     /// and properties that have hints are included.
    //     /// </summary>
    //     public static StructureSerializationMapping ManualMap(Type type, params SerializationValueHint[] hints)
    //     {
    //         var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
    //             .Where(f => !f.IsInitOnly)
    //             .Select(f => new SerializationValue(field: f));
    //         
    //         var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
    //             .Where(p => p.CanRead && p.CanWrite)
    //             .Select(p => new SerializationValue(property: p));
    //         
    //         var mappings = fields.Concat(properties).Where((m) =>
    //         {
    //             var hint = hints.FirstOrDefault(h => h.Name == m.Name);
    //             
    //             if (hint.Equals(default(SerializationValueHint)))
    //                 return false;
    //             
    //             return (m.IsField && hint.Path == SerializationValueLocation.Field) ||
    //                    (m.IsProperty && hint.Path != SerializationValueLocation.Property);
    //         }).ToArray();
    //         
    //         return new StructureSerializationMapping(type, mappings);
    //     }
    // }
}