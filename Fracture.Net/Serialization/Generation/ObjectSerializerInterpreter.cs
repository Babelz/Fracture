using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Fracture.Net.Serialization.Generation.Builders;
using NLog;

namespace Fracture.Net.Serialization.Generation
{
    /// <summary>
    /// Structure that contains serialization context for single type.
    /// </summary>
    public readonly struct ObjectSerializationContext
    {
        #region Properties
        /// <summary>
        /// Gets optional utility bit field serializer if the serializer needs to serialize null values.
        /// </summary>
        public IValueSerializer BitFieldSerializer
        {
            get;
        }
        
        /// <summary>
        /// Gets the count of nullable fields in this serialization context.
        /// </summary>
        public int NullableValuesCount
        {
            get;
        }
        
        /// <summary>
        /// Offset to value serializers index where the first nullable field is at.
        /// </summary>
        public int NullableValuesOffset
        {
            get;
        }
        
        /// <summary>
        /// Gets the value serializers for the type in order they are expected to be executed.
        /// </summary>
        public IReadOnlyList<IValueSerializer> ValueSerializers
        {
            get;
        }
        #endregion

        public ObjectSerializationContext(IReadOnlyList<IValueSerializer> valueSerializers, 
                                          int nullableValuesCount, 
                                          int nullableValuesOffset,
                                          IValueSerializer bitFieldSerializer)
        {
            ValueSerializers     = valueSerializers ?? throw new ArgumentNullException(nameof(valueSerializers));
            NullableValuesCount  = nullableValuesCount;
            NullableValuesOffset = nullableValuesOffset;
            BitFieldSerializer   = bitFieldSerializer;

            if (nullableValuesCount != 0 && bitFieldSerializer == null)
                throw new InvalidOperationException("expecting utility serializers to present for null serialization");
        }
    }

    /// <summary>
    /// Class that wraps dynamic serialization context for single type and provides serialization.
    /// </summary>
    public sealed class ObjectSerializer 
    {
        #region Fields
        private readonly ObjectSerializationContext context; 
        private readonly DynamicSerializeDelegate serialize;
        private readonly DynamicDeserializeDelegate deserialize;
        private readonly DynamicGetSizeFromValueDelegate getSizeFromValue;
        #endregion

        public ObjectSerializer(in ObjectSerializationContext context, 
                                DynamicSerializeDelegate serialize,
                                DynamicDeserializeDelegate deserialize,
                                DynamicGetSizeFromValueDelegate getSizeFromValue)
        {
            this.context          = context;
            this.serialize        = serialize ?? throw new ArgumentNullException(nameof(serialize));
            this.deserialize      = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            this.getSizeFromValue = getSizeFromValue ?? throw new ArgumentNullException(nameof(getSizeFromValue));
        }
        
        /// <summary>
        /// Serializers given object to given buffer using dynamically generated serializer.
        /// </summary>
        public void Serialize(object value, byte[] buffer, int offset)
            => serialize(context, value, buffer, offset);
        
        /// <summary>
        /// Deserializes object from given buffer using dynamically generated deserializer.
        /// </summary>
        public object Deserialize(byte[] buffer, int offset)
            => deserialize(context, buffer, offset);
        
        /// <summary>
        /// Returns the size of the object using dynamically generated resolver.
        /// </summary>
        public ushort GetSizeFromValue(object value)
            => getSizeFromValue(context, value);
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
        public IReadOnlyCollection<SerializationValue> Parameters
        {
            get;
        }
        #endregion

        public ParametrizedActivationOp(ConstructorInfo constructor, IReadOnlyCollection<SerializationValue> parameters)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Parameters  = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
    
        public override string ToString()
            => $"{{ op: {nameof(ParametrizedActivationOp)}, ctor: parametrized, params: {Parameters.Count} }}";
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
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        }

        public override string ToString()
            => $"{{ op: {nameof(DefaultActivationOp)}, ctor: default }}";
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
        #endregion

        public SerializeValueOp(SerializationValue value)
        {
            Value = value;
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
        public IReadOnlyList<IValueSerializer> Serializers
        {
            get;
        }
        #endregion

        public ObjectSerializerProgram(Type type, IEnumerable<ISerializationOp> serializationOps, IEnumerable<ISerializationOp> deserializationOps)
        {
            Type               = type ?? throw new ArgumentNullException(nameof(type));
            SerializationOps   = serializationOps?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(serializationOps));
            DeserializationOps = deserializationOps?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(deserializationOps));

            var serializationOpsSerializers   = GetOpSerializers(SerializationOps);
            var deserializationOpsSerializers = GetOpSerializers(DeserializationOps);
            
            Serializers = serializationOpsSerializers.Intersect(deserializationOpsSerializers).ToList();
            
            if (Serializers.Count != SerializationOps.Count) 
                throw new InvalidOperationException($"serialization programs for type \"{Type.Name}\" have different count of value serializers");
        }   
        
        public static IEnumerable<IValueSerializer> GetOpSerializers(IEnumerable<ISerializationOp> ops)
        {
            foreach (var op in ops)
            {
                switch (op)
                {
                    case ParametrizedActivationOp paop:
                        foreach (var serializer in paop.Parameters.Select(p => ValueSerializerRegistry.GetValueSerializerForType(p.Type)))
                            yield return serializer;
                        break;
                    case SerializeValueOp svop:
                        yield return ValueSerializerRegistry.GetValueSerializerForType(svop.Value.Type);
                        break;
                    default:
                        continue;
                }
            }
        }
    }
    
    /// <summary>
    /// Static class that translates serialization run types to operations and outputs instructions how the type can be serialized and deserialized. 
    /// </summary>
    public static class ObjectSerializerCompiler
    {        
        /// <summary>
        /// Compiles deserialization instructions from given mappings to <see cref="ObjectSerializationProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ISerializationOp> CompileDeserializationOps(in ObjectSerializationMapping mapping)
        {
            var ops = new List<ISerializationOp>();
            
            if (!mapping.Activator.IsDefaultConstructor)
                ops.Add(new ParametrizedActivationOp(mapping.Activator.Constructor, mapping.Activator.Values));
            else
                ops.Add(new DefaultActivationOp(mapping.Activator.Constructor));

            ops.AddRange(mapping.Values.Select(v => (ISerializationOp)new SerializeValueOp(v)));

            return ops;
        }
        
        /// <summary>
        /// Compiles serialization instructions from given mappings to <see cref="ObjectSerializationProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ISerializationOp> CompileSerializationOps(in ObjectSerializationMapping mapping)
            => (mapping.Activator.IsDefaultConstructor ? mapping.Values : mapping.Activator.Values.Concat(mapping.Values)).Select(
                v => (ISerializationOp)new SerializeValueOp(v)
               ).ToList();
        
        /// <summary>
        /// Compiles both serialization and deserialization instructions from given mappings to <see cref="ObjectSerializerProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializerProgram CompileSerializerProgram(ObjectSerializationMapping mapping)
            => new ObjectSerializerProgram(mapping.Type, CompileSerializationOps(mapping), CompileDeserializationOps(mapping));
    }

    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        public static ObjectSerializationContext InterpretObjectSerializationContext(Type type,
                                                                                     IEnumerable<ISerializationOp> ops, 
                                                                                     IReadOnlyList<IValueSerializer> serializers)
        {
            var nullableValuesOffset    = -1;
            var valueOps                = ops.Where(o => o is SerializeValueOp).Cast<SerializeValueOp>().ToList();
            var firstNullableValueIndex = int.MaxValue;
            var lastNullableValueIndex  = int.MinValue;
            
            for (var i = 0; i < valueOps.Count; i++)
            {
                var op = valueOps[i];

                if (!op.Value.IsNullAssignable)
                    continue;
                
                if (nullableValuesOffset < 0) 
                    nullableValuesOffset = i;
                
                if (firstNullableValueIndex > i)
                    firstNullableValueIndex = i;
                
                if (lastNullableValueIndex < i)
                    lastNullableValueIndex = i;
            }
            
            var nullableValuesCount = lastNullableValueIndex < 0 ? 0 : (lastNullableValueIndex - firstNullableValueIndex) + 1;
            
            return new ObjectSerializationContext(serializers, 
                                                  nullableValuesCount, 
                                                  nullableValuesOffset,
                                                  nullableValuesCount != 0 ? ValueSerializerRegistry.GetValueSerializerForType(typeof(BitField)) : null);
        }
        
        public static DynamicDeserializeDelegate InterpretDynamicDeserializeDelegate(in ObjectSerializationContext context,
                                                                                     Type type, 
                                                                                     IReadOnlyList<ISerializationOp> ops)
        {
            // var builder = new DynamicMethod($"Deserialize{program.Type.Name}", 
            //                                 typeof(object), 
            //                                 new [] { typeof(ObjectSerializationContext), typeof(byte[]), typeof(int) }, 
            //                                 true);
            //
            // var il = builder.GetILGenerator();
            //
            // return (DynamicDeserializeDelegate)builder.CreateDelegate(typeof(DynamicDeserializeDelegate));
            return delegate
            {
                return null;
            };
        }

        public static DynamicGetSizeFromValueDelegate InterpretDynamicGetSizeFromValueDelegate(in ObjectSerializationContext context,
                                                                                               Type type, 
                                                                                               IReadOnlyList<ISerializationOp> ops,
                                                                                               DynamicGetSizeFromValueDelegateBuilder builder = null)
        {
            builder ??= new DynamicGetSizeFromValueDelegateBuilder(context, type);
            
            builder.EmitLocals();
            
            builder.EmitSizeOfNullMask();
            
            for (var serializationValueIndex = 0; serializationValueIndex < ops.Count; serializationValueIndex++)
            {
                var op = (SerializeValueOp)ops[serializationValueIndex];

                if (!op.Value.IsValueType) 
                    builder.EmitGetSizeOfNonValueTypeValue(op.Value, serializationValueIndex);
                else if (op.Value.IsNullable)
                    builder.EmitGetSizeOfNullableValue(op.Value, serializationValueIndex);
                else
                    builder.EmitGetSizeOfValue(op.Value, serializationValueIndex);
            }
            
            return builder.Build();
        }

        public static DynamicSerializeDelegate InterpretDynamicSerializeDelegate(in ObjectSerializationContext context,
                                                                                 Type type, 
                                                                                 IReadOnlyList<ISerializationOp> ops,
                                                                                 DynamicSerializeDelegateBuilder builder = null)
        {
            builder ??= new DynamicSerializeDelegateBuilder(context, type);
 
            // Declare locals.
            builder.EmitLocals();
            
            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var serializationValueIndex = 0; serializationValueIndex < ops.Count; serializationValueIndex++)
            {
                var op = (SerializeValueOp)ops[serializationValueIndex];

                if (!op.Value.IsValueType) 
                    builder.EmitSerializeNonValueTypeValue(op.Value, serializationValueIndex);
                else if (op.Value.IsNullable)
                    builder.EmitSerializeNullableValue(op.Value, serializationValueIndex);
                else
                    builder.EmitSerializeValue(op.Value, serializationValueIndex);
            }
            
            return builder.Build();
        }

        public static ObjectSerializer InterpretSerializer(in ObjectSerializerProgram program)
        {
            // Create context based on program. Instructions from serialize program should be usable when interpreting this function.
            var objectSerializationContext = InterpretObjectSerializationContext(program.Type, program.SerializationOps, program.Serializers);
            
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
                objectSerializationContext,
                dynamicSerializeDelegate,
                dynamicDeserializeDelegate,
                dynamicGetSizeFromValueDelegate
            );
        }
        
        public sealed class Vector2
        {
            public float? X;
            public float? Y;
            public float I;
            public float J;
        }
        
        public static void SerializeTestEmit(in ObjectSerializationContext objectSerializationContext, object value, byte[] buffer, int offset)
        {
            var actual = (Vector2)value;
            var serializers = objectSerializationContext.ValueSerializers;
            var currentSerializer = default(IValueSerializer);
            var bitFieldSerializer = new BitFieldSerializer();
            var nullMask = new BitField(1);
            var nullMaskOffset = offset;
            
            offset += bitFieldSerializer.GetSizeFromValue(nullMask);
            
            if (actual.X.HasValue)
            {
                currentSerializer.Serialize(actual.X.Value, buffer, offset);
                offset += currentSerializer.GetSizeFromValue(actual.X.Value);
            }
            else
            {
                nullMask.SetBit(0, true);
            }
            
            if (actual.Y.HasValue)
            {
                currentSerializer.Serialize(actual.Y.Value, buffer, offset);
                offset += currentSerializer.GetSizeFromValue(actual.Y.Value);
            }
            else
            {
                nullMask.SetBit(1, true);
            }
            
            currentSerializer.Serialize(actual.I, buffer, offset);
            offset += currentSerializer.GetSizeFromValue(actual.I);
            
            currentSerializer.Serialize(actual.J, buffer, offset);
            
            bitFieldSerializer.Serialize(nullMask, buffer, nullMaskOffset);
        }
    }
}