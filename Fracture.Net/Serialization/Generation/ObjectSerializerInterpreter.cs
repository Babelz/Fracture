using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Structure that contains serialization value ranges for single type.
    /// </summary>
    public readonly struct ObjectSerializationValueRanges
    {
        #region Properties
        /// <summary>
        /// Gets the count of nullable fields in this serialization context.
        /// </summary>
        public int NullableValuesCount
        {
            get;
        }

        /// <summary>
        /// Gets the total count of expected serialization values count.
        /// </summary>
        public int SerializationValuesCount
        {
            get;
        }
        #endregion

        public ObjectSerializationValueRanges(int nullableValuesCount, int serializationValuesCount)
        {
            NullableValuesCount      = nullableValuesCount;
            SerializationValuesCount = serializationValuesCount;
        }
    }

    /// <summary>
    /// Class that wraps dynamic serialization context for single type and provides serialization.
    /// </summary>
    public sealed class ObjectSerializer
    {
        #region Fields
        private readonly DynamicSerializeDelegate serialize;
        private readonly DynamicDeserializeDelegate deserialize;
        private readonly DynamicGetSizeFromValueDelegate getSizeFromValue;
        #endregion

        public ObjectSerializer(DynamicSerializeDelegate serialize,
                                DynamicDeserializeDelegate deserialize,
                                DynamicGetSizeFromValueDelegate getSizeFromValue)
        {
            this.serialize        = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.deserialize      = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            this.getSizeFromValue = getSizeFromValue ?? throw new ArgumentNullException(nameof(getSizeFromValue));
        }

        /// <summary>
        /// Serializers given object to given buffer using dynamically generated serializer.
        /// </summary>
        public void Serialize(object value, byte [] buffer, int offset)
            => serialize(value, buffer, offset);

        /// <summary>
        /// Deserializes object from given buffer using dynamically generated deserializer.
        /// </summary>
        public object Deserialize(byte [] buffer, int offset)
            => deserialize(buffer, offset);

        /// <summary>
        /// Returns the size of the object using dynamically generated resolver.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => getSizeFromValue(value);
    }

    /// <summary>
    /// Interface for marking structures to represent serialization operations. 
    /// </summary>
    public interface ISerializationOp
    {
        // Marker interface, nothing to implement.
    }

    /// <summary>
    /// Structure representing operation that causes object to be instantiated using specific parametrized constructor when being deserialized. 
    /// </summary>
    public readonly struct ParametrizedActivationOp : ISerializationOp
    {
        #region Properties
        /// <summary>
        /// Gets the parametrized constructor used for object activation.
        /// </summary>
        public ConstructorInfo Constructor
        {
            get;
        }

        /// <summary>
        /// Gets the serialization values that are expected by the parametrized constructor.
        /// </summary>
        public IReadOnlyCollection<SerializeValueOp> ParameterValueOps
        {
            get;
        }
        #endregion

        public ParametrizedActivationOp(ConstructorInfo constructor, IReadOnlyCollection<SerializeValueOp> parameters)
        {
            Constructor       = constructor ?? throw new ArgumentNullException(nameof(constructor));
            ParameterValueOps = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public override string ToString()
            => $"{{ op: {nameof(ParametrizedActivationOp)}, ctor: parametrized, params: {ParameterValueOps.Count} }}";
    }

    /// <summary>
    /// Structure representing operation that causes object to be instantiated using default parameterless constructor when being deserialized.
    /// </summary>
    public readonly struct DefaultActivationOp : ISerializationOp
    {
        #region Properties
        /// <summary>
        /// Gets the parameterless default constructor.
        /// </summary>
        public ConstructorInfo Constructor
        {
            get;
        }
        #endregion

        public DefaultActivationOp(ConstructorInfo constructor)
            => Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));

        public override string ToString()
            => $"{{ op: {nameof(DefaultActivationOp)}, ctor: default }}";
    }

    public readonly struct IndirectActivationOp : ISerializationOp
    {
        #region Properties
        public ObjectActivationDelegate Activator
        {
            get;
        }
        #endregion

        public IndirectActivationOp(ObjectActivationDelegate activator)
            => Activator = activator ?? throw new ArgumentNullException(nameof(activator));

        public override string ToString()
            => $"{{ op: {nameof(IndirectActivationOp)}, ctor: none, activator: {Activator} }}";
    }

    /// <summary>
    /// Structure representing serialization value serialization operation. Depending on the context the operation is either interpreted as property/field read or write operation.
    /// </summary>
    public readonly struct SerializeValueOp : ISerializationOp
    {
        #region Properties
        /// <summary>
        /// Gets the serialization value associated with this serialization operation.
        /// </summary>
        public SerializationValue Value
        {
            get;
        }

        /// <summary>
        /// Gets the value serializer type for serializing and deserializing value associated with this operation.
        /// </summary>
        public Type ValueSerializerType
        {
            get;
        }
        #endregion

        public SerializeValueOp(SerializationValue value, Type valueSerializerType)
        {
            Value               = value;
            ValueSerializerType = valueSerializerType ?? throw new ArgumentNullException(nameof(valueSerializerType));
        }

        public override string ToString()
            => $"{{ op: {nameof(SerializeValueOp)}, value: {Value.Name}, path: {(Value.IsProperty ? "property" : "field")} }}";
    }

    /// <summary>
    /// Structure that represents full serialization program. This structure contains instructions for generating both the serialize and deserialize functions.
    /// </summary>
    public readonly struct ObjectSerializerProgram
    {
        #region Properties
        /// <summary>
        /// Gets the type that this program is target. Dynamic serializer will be created for this type.
        /// </summary>
        public Type Type
        {
            get;
        }

        /// <summary>
        /// Gets the ops for generating serialize function.
        /// </summary>
        public IReadOnlyList<ISerializationOp> SerializationOps
        {
            get;
        }

        /// <summary>
        /// Gets the ops for generating deserialization function.
        /// </summary>
        public IReadOnlyList<ISerializationOp> DeserializationOps
        {
            get;
        }

        /// <summary>
        /// Gets the program serializers for the program.
        /// </summary>
        public IReadOnlyList<Type> ValueSerializerTypes
        {
            get;
        }

        public IReadOnlyList<Type> SerializationTypes
        {
            get;
        }
        #endregion

        public ObjectSerializerProgram(Type type,
                                       IEnumerable<ISerializationOp> serializationOps,
                                       IEnumerable<ISerializationOp> deserializationOps,
                                       IEnumerable<Type> serializationTypes)
        {
            Type               = type ?? throw new ArgumentNullException(nameof(type));
            SerializationOps   = serializationOps?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(serializationOps));
            DeserializationOps = deserializationOps?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(deserializationOps));
            SerializationTypes = serializationTypes?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(serializationTypes));

            var serializationOpsSerializers   = GetOpValueSerializerTypes(SerializationOps).ToList();
            var deserializationOpsSerializers = GetOpValueSerializerTypes(DeserializationOps).ToList();

            if (serializationOpsSerializers.Count != deserializationOpsSerializers.Count)
                throw new InvalidOperationException($"serialization programs for type \"{Type.Name}\" have different count of value serializers");

            ValueSerializerTypes = serializationOpsSerializers.Intersect(deserializationOpsSerializers).ToList();
        }

        public static IEnumerable<Type> GetOpValueSerializerTypes(IEnumerable<ISerializationOp> ops)
        {
            foreach (var op in ops)
            {
                switch (op)
                {
                    case ParametrizedActivationOp paop:
                        foreach (var serializer in paop.ParameterValueOps.Select(p => p.ValueSerializerType))
                            yield return serializer;

                        break;
                    case SerializeValueOp svop:
                        yield return ValueSerializerRegistry.GetValueSerializerForRunType(svop.Value.Type);

                        break;
                    default:
                        continue;
                }
            }
        }
    }

    /// <summary>
    /// Static class that provides pre-interpretation steps for compiling dynamic object serializers. 
    /// </summary>
    public static class ObjectSerializerAnalyzer
    {
        /// <summary>
        /// Analyzes given serialization type and extends it when needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Analyze(Type serializationType)
        {
            if (serializationType.IsGenericType)
                Analyze(serializationType.GetGenericArguments());

            if (serializationType.IsArray)
                Analyze(serializationType.GetElementType());

            var valueSerializerType = ValueSerializerRegistry.GetValueSerializerForRunType(serializationType);

            if (!ValueSerializerRegistry.IsExtendableValueSerializer(valueSerializerType))
                return;

            if (!ValueSerializerRegistry.CreateCanExtendTypeDelegate(valueSerializerType)(serializationType))
                return;

            ValueSerializerRegistry.CreateExtendTypeDelegate(valueSerializerType)(serializationType);
        }

        /// <summary>
        /// Iterates trough given value serializer types and analyzes them.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Analyze(IEnumerable<Type> serializationTypes)
        {
            foreach (var serializationType in serializationTypes)
                Analyze(serializationType);
        }
    }

    /// <summary>
    /// Static class that translates serialization run types to operations and outputs instructions how the type can be serialized and deserialized. 
    /// </summary>
    public static class ObjectSerializerCompiler
    {
        /// <summary>
        /// Compiles deserialization instructions from given mappings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ISerializationOp> CompileDeserializationOps(in ObjectSerializationMapping mapping)
        {
            var ops = new List<ISerializationOp>();

            if (mapping.Activator.IsCallbackInitializer)
                ops.Add(new IndirectActivationOp(mapping.Activator.Callback));
            else if (!mapping.Activator.IsDefaultConstructor)
                ops.Add(new ParametrizedActivationOp(
                            mapping.Activator.Constructor,
                            mapping.Activator.Values.Select(v => new SerializeValueOp(
                                                                v,
                                                                ValueSerializerRegistry.GetValueSerializerForRunType(v.Type)))
                                   .ToList()
                                   .AsReadOnly()
                        )
                );
            else if (!mapping.Activator.IsStructInitializer)
                ops.Add(new DefaultActivationOp(mapping.Activator.Constructor));

            ops.AddRange(mapping.Values.Select(v => (ISerializationOp)new SerializeValueOp(
                                                   v,
                                                   ValueSerializerRegistry.GetValueSerializerForRunType(v.Type))
                         )
            );

            return ops;
        }

        /// <summary>
        /// Compiles serialization instructions from given mappings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ISerializationOp> CompileSerializationOps(in ObjectSerializationMapping mapping)
            => (mapping.Activator.IsDefaultConstructor || mapping.Activator.IsCallbackInitializer
                    ? mapping.Values
                    : mapping.Activator.Values.Concat(mapping.Values)).Select(
                                                                           v => (ISerializationOp)new SerializeValueOp(v,
                                                                               ValueSerializerRegistry.GetValueSerializerForRunType(v.Type))
                                                                       )
                                                                      .ToList();

        /// <summary>
        /// Compiles both serialization and deserialization instructions from given mappings.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializerProgram CompileSerializerProgram(ObjectSerializationMapping mapping)
            => new ObjectSerializerProgram(mapping.Type,
                                           CompileSerializationOps(mapping),
                                           CompileDeserializationOps(mapping),
                                           mapping.Values.Select(v => v.Type).Distinct());
    }

    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
        public static ObjectSerializationValueRanges InterpretObjectSerializationValueRanges(Type type, IEnumerable<ISerializationOp> ops)
        {
            var valueOps                = ops.Where(o => o is SerializeValueOp).Cast<SerializeValueOp>().ToList();
            var firstNullableValueIndex = int.MaxValue;
            var lastNullableValueIndex  = int.MinValue;

            for (var i = 0; i < valueOps.Count; i++)
            {
                var op = valueOps[i];

                if (!op.Value.IsNullAssignable)
                    continue;

                if (firstNullableValueIndex > i)
                    firstNullableValueIndex = i;

                if (lastNullableValueIndex < i)
                    lastNullableValueIndex = i;
            }

            var nullableValuesCount = lastNullableValueIndex < 0 ? 0 : (lastNullableValueIndex - firstNullableValueIndex) + 1;

            return new ObjectSerializationValueRanges(nullableValuesCount, valueOps.Count);
        }

        public static DynamicDeserializeDelegate InterpretDynamicDeserializeDelegate(in ObjectSerializationValueRanges valueRanges,
                                                                                     Type type,
                                                                                     IEnumerable<ISerializationOp> ops)
        {
            var serializationValueIndex = 0;

            var builder = ops.OfType<IndirectActivationOp>().Any()
                              ? DynamicDeserializeDelegateBuilder.CreateWithIndirectActivation(valueRanges, type)
                              : DynamicDeserializeDelegateBuilder.CreateWithDirectActivation(valueRanges, type);

            builder.EmitLocals();

            foreach (var op in ops)
            {
                switch (op)
                {
                    case SerializeValueOp svop:
                        if (svop.Value.IsNullAssignable)
                            builder.EmitDeserializeNullableValue(svop.Value, svop.ValueSerializerType, serializationValueIndex++);
                        else
                            builder.EmitDeserializeValue(svop.Value, svop.ValueSerializerType, serializationValueIndex++);

                        break;
                    case DefaultActivationOp daop:
                        builder.EmitActivation(daop.Constructor);

                        break;
                    case ParametrizedActivationOp paop:
                        foreach (var parameter in paop.ParameterValueOps)
                        {
                            if (parameter.Value.IsNullAssignable)
                                builder.EmitLoadNullableValue(parameter.Value, parameter.ValueSerializerType);
                            else
                                builder.EmitLoadValue(parameter.Value, parameter.ValueSerializerType);
                        }

                        builder.EmitActivation(paop.Constructor);

                        break;
                    case IndirectActivationOp iaop:
                        builder.EmitActivation(iaop.Activator);

                        break;
                }
            }

            return builder.Build();
        }

        public static DynamicGetSizeFromValueDelegate InterpretDynamicGetSizeFromValueDelegate(in ObjectSerializationValueRanges valueRanges,
                                                                                               Type type,
                                                                                               IReadOnlyList<ISerializationOp> ops)
        {
            var builder = new DynamicGetSizeFromValueDelegateBuilder(valueRanges, type);

            builder.EmitLocals();

            builder.EmitSizeOfNullMask();

            for (var serializationValueIndex = 0; serializationValueIndex < ops.Count; serializationValueIndex++)
            {
                var op = (SerializeValueOp)ops[serializationValueIndex];

                if (!op.Value.IsValueType)
                    builder.EmitGetSizeOfNonValueTypeValue(op.Value, op.ValueSerializerType);
                else if (op.Value.IsNullable)
                    builder.EmitGetSizeOfNullableValue(op.Value, op.ValueSerializerType);
                else
                    builder.EmitGetSizeOfValue(op.Value, op.ValueSerializerType);
            }

            return builder.Build();
        }

        public static DynamicSerializeDelegate InterpretDynamicSerializeDelegate(in ObjectSerializationValueRanges valueRanges,
                                                                                 Type type,
                                                                                 IReadOnlyList<ISerializationOp> ops)
        {
            var builder = new DynamicSerializeDelegateBuilder(valueRanges, type);

            // Declare locals.
            builder.EmitLocals();

            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var serializationValueIndex = 0; serializationValueIndex < ops.Count; serializationValueIndex++)
            {
                var op = (SerializeValueOp)ops[serializationValueIndex];

                if (!op.Value.IsValueType)
                    builder.EmitSerializeNonValueTypeValue(op.Value, op.ValueSerializerType, serializationValueIndex);
                else if (op.Value.IsNullable)
                    builder.EmitSerializeNullableValue(op.Value, op.ValueSerializerType, serializationValueIndex);
                else
                    builder.EmitSerializeValue(op.Value, op.ValueSerializerType, serializationValueIndex);
            }

            return builder.Build();
        }

        public static ObjectSerializer InterpretSerializer(in ObjectSerializerProgram program)
        {
            ObjectSerializerAnalyzer.Analyze(program.SerializationTypes);

            // Create context based on program. Instructions from serialize program should be usable when interpreting this function.
            var objectSerializationContext = InterpretObjectSerializationValueRanges(program.Type, program.SerializationOps);

            // Create dynamic serialization function.
            var dynamicSerializeDelegate = InterpretDynamicSerializeDelegate(objectSerializationContext,
                                                                             program.Type,
                                                                             program.SerializationOps);

            // Create dynamic deserialization function.
            var dynamicDeserializeDelegate = InterpretDynamicDeserializeDelegate(objectSerializationContext,
                                                                                 program.Type,
                                                                                 program.DeserializationOps);

            // Create dynamic get size function. Instructions from serialize program should be usable when interpreting this function.
            var dynamicGetSizeFromValueDelegate = InterpretDynamicGetSizeFromValueDelegate(objectSerializationContext,
                                                                                           program.Type,
                                                                                           program.SerializationOps);

            return new ObjectSerializer(
                dynamicSerializeDelegate,
                dynamicDeserializeDelegate,
                dynamicGetSizeFromValueDelegate
            );
        }
    }
}