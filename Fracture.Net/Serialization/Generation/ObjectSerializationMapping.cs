using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        Field,
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
        public string ParameterName
        {
            get;
        }

        /// <summary>
        /// Gets the value that this argument is pointing to. When the object is being serialized the serializer
        /// will read the value for activation based on this value hint.
        /// </summary>
        public SerializationValueHint Value
        {
            get;
        }

        /// <summary>
        /// Optional type parameter hint used in cases where constructor with same parameter name signature exists but with different types.
        /// </summary>
        public Type Type
        {
            get;
        }
        #endregion

        private ObjectActivationHint(string parameterName, SerializationValueHint value, Type type = null)
        {
            ParameterName = !string.IsNullOrEmpty(parameterName) ? parameterName : throw new ArgumentNullException(nameof(parameterName));
            Value         = value;
            Type          = type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectActivationHint Field(string parameterName, string fieldName, Type type = null)
            => new ObjectActivationHint(parameterName, SerializationValueHint.Field(fieldName), type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectActivationHint Property(string parameterName, string propertyName, Type type = null)
            => new ObjectActivationHint(parameterName, SerializationValueHint.Property(propertyName), type);
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

        private SerializationValueHint(string name, SerializationValueLocation path)
        {
            Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Path = path;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationValueHint Field(string name)
            => new SerializationValueHint(name, SerializationValueLocation.Field);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SerializationValueHint Property(string name)
            => new SerializationValueHint(name, SerializationValueLocation.Property);
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
        /// Returns the property or field type.
        /// </summary>
        public Type Type => IsField ? Field.FieldType : Property.PropertyType;

        /// <summary>
        /// Returns boolean declaring whether this mapping points to a field.
        /// </summary>
        public bool IsField => Field != null;

        /// <summary>
        /// Returns boolean declaring whether this mapping points to a property.
        /// </summary>
        public bool IsProperty => Property != null;

        /// <summary>
        /// Returns boolean declaring whether this value type is primitive value type.
        /// </summary>
        public bool IsValueType => Type.IsValueType;

        /// <summary>
        /// Returns boolean declaring whether this value type can be null.
        /// </summary>
        public bool IsNullable => Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// Returns boolean declaring whether null can be assigned to this value.
        /// </summary>
        public bool IsNullAssignable => !IsValueType || IsNullable;
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

        public override string ToString()
            => IsProperty ? $"property: {Property.PropertyType.Name} {Property.Name}" : $"field: {Field.FieldType.Name} {Field.Name}";
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
        /// Gets the activator callback delegate that should be used for object activation.
        /// </summary>
        public ObjectActivationDelegate Callback
        {
            get;
        }

        /// <summary>
        /// Gets the optional serialization values if non-default constructor is being used for activation.
        /// </summary>
        public IReadOnlyCollection<SerializationValue> Values
        {
            get;
        }

        /// <summary>
        /// Gets boolean declaring whether this object activator is using the default constructor.
        /// </summary>
        public bool IsDefaultConstructor => !IsCallbackInitializer && Values.Count == 0;

        /// <summary>
        /// Gets boolean declaring whether this object activator is default structure initializer.
        /// </summary>
        public bool IsStructInitializer => IsDefaultConstructor && Constructor == null;

        /// <summary>
        /// Gets boolean declaring whether this object activator uses delegate callback for creating the objects.
        /// </summary>
        public bool IsCallbackInitializer => Callback != null;
        #endregion

        public ObjectActivator(ConstructorInfo constructor, IReadOnlyCollection<SerializationValue> values)
        {
            Callback    = null;
            Constructor = constructor;
            Values      = values;
        }

        public ObjectActivator(ObjectActivationDelegate callback)
        {
            Callback    = callback;
            Constructor = null;
            Values      = null;
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
    /// Delegate for creating serialization object activation callbacks. These callbacks are provided for the serializers to get empty object to which the data
    /// will be deserialized to.
    /// </summary>
    public delegate object ObjectActivationDelegate();

    /// <summary>
    /// Class for mapping objects to their intermediate serialization instructions. Mapper provides builder interface for mapping
    /// objects for serialization.
    /// </summary>
    public sealed class ObjectSerializationMapper
    {
        #region Fields
        private readonly Type serializationType;

        private readonly HashSet<string> excludedProperties;
        private readonly HashSet<string> excludedFields;

        private readonly List<SerializationValueHint> serializationValueHints;
        private readonly List<ObjectActivationHint>   objectActivationHints;

        private ObjectActivationDelegate activator;

        private bool discoverPublicFields;
        private bool discoverPublicProperties;
        #endregion

        private ObjectSerializationMapper(Type serializationType)
        {
            this.serializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));

            serializationValueHints = new List<SerializationValueHint>();
            objectActivationHints   = new List<ObjectActivationHint>();

            excludedProperties = new HashSet<string>();
            excludedFields     = new HashSet<string>();
        }

        private void AssertSerializationTypeIsValidForSerialization()
        {
            if (serializationType == null)
                throw new InvalidOperationException("can't map null type");

            if (serializationType.IsInterface)
                throw new InvalidOperationException($"can't map interface type \"{serializationType.Name}\"");

            if (serializationType.IsAbstract)
                throw new InvalidOperationException($"can't map abstract type \"{serializationType.Name}\"");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertFieldIsValidForSerialization(FieldInfo fieldInfo, string fieldNameHint, bool usedInActivation = false)
        {
            if (fieldInfo == null)
                throw new InvalidOperationException($"no field matches serialization field hint \"{fieldNameHint}\"");

            if (fieldInfo.IsInitOnly && !usedInActivation)
                throw new InvalidOperationException($"can't serialize readonly field \"{fieldInfo.Name}\"");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertPropertyIsValidForSerialization(PropertyInfo property, string propertyNameHint, bool usedInActivation = false)
        {
            if (property == null)
                throw new InvalidOperationException($"no property matches serialization field hint \"{propertyNameHint}\"");

            if (!property.CanWrite && !usedInActivation)
                throw new InvalidOperationException($"can't map to property \"{property.Name}\" because it can't be used for writing");

            if (!property.CanRead)
                throw new InvalidOperationException($"can't map to property \"{property.Name}\" because it can't be used for reading");
        }

        private ConstructorInfo GetObjectActivationConstructor()
        {
            ConstructorInfo constructor = null;

            if (objectActivationHints.Count == 0)
            {
                // Ensure type has parameterless constructor.
                constructor = serializationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                               .FirstOrDefault(c => c.GetParameters().Length == 0);

                if (!serializationType.IsValueType && constructor == null)
                    throw new InvalidOperationException($"type {serializationType.Name} has no parameterless constructor");
            }
            else
            {
                var candidates = serializationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                  .Where(c => c.GetParameters().Length == objectActivationHints.Count)
                                                  .ToArray();

                if (objectActivationHints.Any(h => h.Type != null))
                {
                    if (objectActivationHints.Count(h => h.Type != null) != objectActivationHints.Count)
                        throw new InvalidOperationException("type hints are used but all activation hints don't have it");

                    candidates = candidates.Where(c => c.GetParameters()
                                                        .Select(p => p.ParameterType)
                                                        .SequenceEqual(objectActivationHints.Select(h => h.Type)))
                                           .ToArray();
                }

                // Go trough all arguments and make sure types match.
                var expectedParameterNames = objectActivationHints.Select(h => h.ParameterName).ToArray();

                foreach (var candidate in candidates)
                {
                    var candidateParameterNames = candidate.GetParameters().Select(p => p.Name);

                    if (!expectedParameterNames.SequenceEqual(candidateParameterNames))
                        continue;

                    constructor = candidate;

                    break;
                }

                if (constructor == null)
                {
                    throw new InvalidOperationException($"type {serializationType.Name} has no constructor " +
                                                        $"that accepts {objectActivationHints.Count} arguments, this might also be caused by mismatched" +
                                                        "binding names");
                }
            }

            return constructor;
        }

        private IEnumerable<SerializationValue> GetObjectActivationValues()
        {
            foreach (var objectActivationHint in objectActivationHints)
                if (objectActivationHint.Value.Path == SerializationValueLocation.Field)
                {
                    // Make sure the serialization field is valid.
                    var serializationTypeField = serializationType.GetField(objectActivationHint.Value.Name,
                                                                            BindingFlags.Public |
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.Instance);

                    AssertFieldIsValidForSerialization(serializationTypeField, objectActivationHint.Value.Name, true);

                    yield return new SerializationValue(serializationTypeField);
                }
                else
                {
                    // Make sure serialization property is valid.
                    var serializationTypeProperty = serializationType.GetProperty(objectActivationHint.Value.Name,
                                                                                  BindingFlags.Public |
                                                                                  BindingFlags.NonPublic |
                                                                                  BindingFlags.Instance);

                    AssertPropertyIsValidForSerialization(serializationTypeProperty, objectActivationHint.Value.Name, true);

                    yield return new SerializationValue(property: serializationTypeProperty);
                }
        }

        private IEnumerable<SerializationValue> GetSerializationFieldValues()
        {
            var serializationTypeFields = serializationType.GetFields(BindingFlags.Instance |
                                                                      BindingFlags.Public |
                                                                      BindingFlags.NonPublic);

            var serializationFieldHints = serializationValueHints.Where(h => h.Path == SerializationValueLocation.Field);

            foreach (var serializationFieldHint in serializationFieldHints)
            {
                var serializationTypeField = serializationTypeFields.FirstOrDefault(f => f.Name == serializationFieldHint.Name);

                AssertFieldIsValidForSerialization(serializationTypeField, serializationFieldHint.Name);

                yield return new SerializationValue(serializationTypeField);
            }
        }

        private IEnumerable<SerializationValue> GetSerializationPropertyValues()
        {
            var serializationTypeProperties = serializationType.GetProperties(BindingFlags.Instance |
                                                                              BindingFlags.Public |
                                                                              BindingFlags.NonPublic);

            var serializationPropertyHints = serializationValueHints.Where(h => h.Path == SerializationValueLocation.Property);

            foreach (var serializationPropertyHint in serializationPropertyHints)
            {
                var serializationTypeProperty = serializationTypeProperties.FirstOrDefault(f => f.Name == serializationPropertyHint.Name);

                AssertPropertyIsValidForSerialization(serializationTypeProperty, serializationPropertyHint.Name);

                yield return new SerializationValue(property: serializationTypeProperty);
            }
        }

        private void DiscoverPublicFieldHints()
        {
            if (!discoverPublicFields)
                return;

            var serializationTypeFields = serializationType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                                           .Where(f => !serializationValueHints.Any(h => h.Name == f.Name &&
                                                                                                         h.Path == SerializationValueLocation.Field));

            serializationValueHints.AddRange(serializationTypeFields.Where(f => !excludedFields.Contains(f.Name))
                                                                    .Select(f => SerializationValueHint.Field(f.Name)));
        }

        private void DiscoverPublicPropertyHints()
        {
            if (!discoverPublicProperties)
                return;

            var serializationTypeProperties = serializationType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                               .Where(p => !serializationValueHints.Any(h => h.Name == p.Name &&
                                                                                                             h.Path == SerializationValueLocation.Property));

            serializationValueHints.AddRange(serializationTypeProperties.Where(p => !excludedProperties.Contains(p.Name))
                                                                        .Select(p => SerializationValueHint.Property(p.Name)));
        }

        private void RemoveActivationValueHints()
            => serializationValueHints.RemoveAll(h => objectActivationHints.Any(a => a.Value.Name == h.Name));

        private ObjectActivator GetObjectActivator()
            => activator != null ? new ObjectActivator(activator) : new ObjectActivator(GetObjectActivationConstructor(), GetObjectActivationValues().ToList().AsReadOnly());

        /// <summary>
        /// Directs the builder to map the types constructor that matches given hints.
        /// </summary>
        public ObjectSerializationMapper ParametrizedActivation(params ObjectActivationHint[] hints)
        {
            if (hints == null)
                throw new ArgumentNullException(nameof(hints));

            if (activator != null)
                throw new InvalidOperationException("mapping already has activator callback defined");

            objectActivationHints.AddRange(hints);

            return this;
        }

        public ObjectSerializationMapper IndirectActivation(ObjectActivationDelegate activator)
        {
            if (objectActivationHints.Count != 0)
                throw new InvalidOperationException("mapping already has parametrized activation defined");

            this.activator = activator;

            return this;
        }

        /// <summary>
        /// Directs the builder to map types all fields and properties based on given hints.
        /// </summary>
        public ObjectSerializationMapper Values(params SerializationValueHint[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            serializationValueHints.AddRange(values);

            return this;
        }

        /// <summary>
        /// Directs the builder to map all public fields of the type.
        /// </summary>
        public ObjectSerializationMapper PublicFields(params string[] excludeFields)
        {
            discoverPublicFields = true;

            if (excludeFields != null)
                Array.ForEach(excludeFields, (s) => excludedFields.Add(s));

            return this;
        }

        /// <summary>
        /// Directs the builder to map all public properties of the type.
        /// </summary>
        public ObjectSerializationMapper PublicProperties(params string[] excludeProperties)
        {
            discoverPublicProperties = true;

            if (excludeProperties != null)
                Array.ForEach(excludeProperties, (s) => excludedProperties.Add(s));

            return this;
        }

        /// <summary>
        /// Builds the mapping based on received configuration and returns it to the caller.
        /// </summary>
        public ObjectSerializationMapping Map()
        {
            // Ensure type exists.
            AssertSerializationTypeIsValidForSerialization();

            // Auto discover any properties or fields if enabled.
            DiscoverPublicFieldHints();
            DiscoverPublicPropertyHints();

            // Ensure activation is possible.
            var objectActivator = GetObjectActivator();

            // Remove all fields and properties that are mapped by the object activator.
            RemoveActivationValueHints();

            // Ensure properties and fields exist, contact all serialization values to single list. Ensure that activation values are first to be serialized
            // and they are in order.
            var serializationPropertyValues = GetSerializationPropertyValues();
            var serializationFieldValues    = GetSerializationFieldValues();
            var serializationValues         = serializationPropertyValues.Concat(serializationFieldValues).ToList().AsReadOnly();

            return new ObjectSerializationMapping(serializationType,
                                                  serializationValues,
                                                  objectActivator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializationMapper ForType(Type type)
            => new ObjectSerializationMapper(type);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializationMapper ForType<T>()
            => new ObjectSerializationMapper(typeof(T));
    }
}