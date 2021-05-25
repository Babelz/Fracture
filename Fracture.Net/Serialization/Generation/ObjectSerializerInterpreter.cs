using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
        }
    }
    
    /// <summary>
    /// Delegate for wrapping serialization functions.
    /// </summary>
    public delegate void DynamicSerializeDelegate(in ObjectSerializationContext context, object value, byte[] buffer, int offset);
    
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object DynamicDeserializeDelegate(in ObjectSerializationContext context, byte[] buffer, int offset);
    
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationContext context, object value);
    
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
    /// Structure representing serialization field operation. Depending on the context the operation is either interpreted as field read or write operation.
    /// </summary>
    public readonly struct SerializationFieldOp : ISerializationOp
    {
        #region Properties
        /// <summary>
        /// Gets the serialization value associated with this field operation. This serialization value is guaranteed to be a field. 
        /// </summary>
        public SerializationValue Value
        {
            get;
        }
        #endregion

        public SerializationFieldOp(in SerializationValue value)
        {
            Value = value;
            
            if (!Value.IsField)
                throw new InvalidEnumArgumentException("excepting field serialization value"); 
        }

        public override string ToString()
            => $"{{ op: {nameof(SerializationFieldOp)}, prop: {Value.Name} }}";
    }
    
    /// <summary>
    /// Structure representing serialization property operation. Depending on the context the operation is either interpreted as property read or write operation.
    /// </summary>
    public readonly struct SerializationPropertyOp : ISerializationOp
    {
        #region Properties
        /// <summary>
        /// Gets the serialization value associated with this property operation. This serialization value is guaranteed to be a property. 
        /// </summary>
        public SerializationValue Value
        {
            get;
        }
        #endregion

        public SerializationPropertyOp(SerializationValue value)
        {
            Value = value;
            
            if (!Value.IsProperty)
                throw new InvalidEnumArgumentException("excepting property serialization value"); 
        }
        
        public override string ToString()
            => $"{{ op: {nameof(SerializationPropertyOp)}, prop: {Value.Name} }}";
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
                    case SerializationFieldOp sfop:
                        yield return ValueSerializerRegistry.GetValueSerializerForType(sfop.Value.Type);
                        break;
                    case SerializationPropertyOp spop:
                        yield return ValueSerializerRegistry.GetValueSerializerForType(spop.Value.Type);
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

            ops.AddRange(mapping.Values.Select(v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)));

            return ops;
        }
        
        /// <summary>
        /// Compiles serialization instructions from given mappings to <see cref="ObjectSerializationProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<ISerializationOp> CompileSerializationOps(in ObjectSerializationMapping mapping)
            => (mapping.Activator.IsDefaultConstructor ? mapping.Values : mapping.Activator.Values.Concat(mapping.Values)).Select(
                    v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)
               ).ToList();
        
        
        /// <summary>
        /// Compiles both serialization and deserialization instructions from given mappings to <see cref="ObjectSerializerProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializerProgram CompileSerializerProgram(ObjectSerializationMapping mapping)
            => new ObjectSerializerProgram(mapping.Type, CompileSerializationOps(mapping), CompileDeserializationOps(mapping));
    }

    /// <summary>
    /// Class that provides dynamic functions building for dynamic serialization functions. 
    /// </summary>
    public sealed class DynamicSerializeDelegateBuilder
    {
        #region Constant fields
        private const byte LocalCurrentSerializer = 0;
        private const byte LocalSerializers       = 1;
        private const byte LocalActual            = 2;
        
        private const byte LocalIsNull             = 3;
        private const byte LocalNullMask           = 4;
        private const byte LocalNullMaskOffset     = 6;
        private const byte LocalBitFieldSerializer = 5;
        
        private const int MaxLocals = 7;
        #endregion
        
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        // Local variables that are accessed by indices.
        private readonly LocalBuilder[] locals;
        
        // Local variables that are accessed by byte.
        private readonly Dictionary<Type, LocalBuilder> nullables;
        
        private readonly DynamicMethod dynamicMethod;
        private readonly Type serializationType;
        
        private int nullableFieldIndex;
        #endregion

        public DynamicSerializeDelegateBuilder(Type serializationType)
        {
            this.serializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
         
            dynamicMethod = new DynamicMethod(
                $"Serialize{this.serializationType.Name}", 
                typeof(void), 
                new []
                {
                   typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                   typeof(object),                                     // Argument 1.
                   typeof(byte[]),                                     // Argument 2.
                   typeof(int)                                         // Argument 3.
                },
                true
            );
            
            locals    = new LocalBuilder[MaxLocals];
            nullables = new Dictionary<Type, LocalBuilder>();
        }
        
        /// <summary>
        /// Emits instructions for getting the next value serializer for serialization.
        ///
        /// Translates roughly to:
        ///     serializer = serializers[serializationValueIndex];
        /// </summary>
        private void EmitStoreNextSerializer(int serializationValueIndex)
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Load local 'serializers' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalSerializers]);
            // Get current value serializer, push current serializer index to stack.
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex);
            // Push serializer at index to stack.
            il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<IValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
            // Store current serializer to local.
            il.Emit(OpCodes.Stloc, locals[LocalCurrentSerializer]); 
            // Serialize field, push 'serializer' to stack from local.
            il.Emit(OpCodes.Ldloc, locals[LocalCurrentSerializer]);
        }
        
        /// <summary>
        /// Emits instructions for incrementing the offset value after field has been serialized.
        ///
        /// Translates roughly to:
        ///     offset += serializer.GetSizeFromValue(actual.[op-field-name]);
        /// </summary>
        private void EmitIncrementFieldSerializationOffset(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            if (serializationValueIndex + 1 >= serializationValueCount) return;
            
            var il = dynamicMethod.GetILGenerator();
            
            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                          
            // Push 'serializer' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalCurrentSerializer]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalActual]);                                                          
            // Push last serialization value to stack.
            il.Emit(OpCodes.Ldfld, op.Value.Field);                                          
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                             
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
        }
        
        /// <summary>
        /// Emits instructions for incrementing the offset value after property has been serialized.
        ///
        /// Translates roughly to:
        ///     offset += serializer.GetSizeFromValue(actual.[op-property-name]);
        /// </summary>
        private void EmitIncrementPropertySerializationOffset(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            if (serializationValueIndex + 1 >= serializationValueCount) return;
            
            var il = dynamicMethod.GetILGenerator();

            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                          
            // Push 'serializer' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalCurrentSerializer]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalActual]);                                                          
            // Push last serialization value to stack.
            il.Emit(OpCodes.Callvirt, op.Value.Property.GetMethod);                                          
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                             
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
        }
        
        public void EmitSerializeNonValueTypeField(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            throw new NotImplementedException();
        }
        
        public void EmitSerializeNullableValueTypeField(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            var il = dynamicMethod.GetILGenerator();
            
            if (!nullables.TryGetValue(op.Value.Field.FieldType, out var localNullable))
            {
                // Declare new local for the new nullable type.
                localNullable = il.DeclareLocal(op.Value.Field.FieldType);
                
                nullables.Add(op.Value.Field.FieldType, localNullable);
            }
            
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalActual]);
            // Push serialization value to stack.
            il.Emit(OpCodes.Ldfld, op.Value.Field);
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, op.Value.Field.FieldType.GetProperty("HasValue")!.GetMethod);
            // Store to local 'isNull'.
            il.Emit(OpCodes.Stloc_S, locals[LocalIsNull]); 
            il.Emit(OpCodes.Ldloc_S, locals[LocalIsNull]); 
            
            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            // Push current serializer to stack.
            il.Emit(OpCodes.Ldloc, locals[LocalCurrentSerializer]);
            // Push local 'actual' to stack. 
            il.Emit(OpCodes.Ldloc, locals[LocalActual]);
            // Push value from the nullable field to stack boxed.
            il.Emit(OpCodes.Ldfld, op.Value.Field);
            il.Emit(OpCodes.Call, op.Value.Field.FieldType.GetProperty("Value")!.GetMethod);
            il.Emit(OpCodes.Box, op.Value.Field.FieldType.GenericTypeArguments[0]);
            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);
            // Push arguments 'offset', 'currentSerializer' and 'actual' to stack.
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldloc_S, locals[LocalCurrentSerializer]);
            il.Emit(OpCodes.Ldloc_S, locals[LocalActual]);
            // Push value from the nullable field to stack boxed.
            il.Emit(OpCodes.Ldfld, op.Value.Field);
            il.Emit(OpCodes.Call, op.Value.Field.FieldType.GetProperty("Value")!.GetMethod);
            il.Emit(OpCodes.Box, op.Value.Field.FieldType.GenericTypeArguments[0]);
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
            // Advance offset by the size of the value.
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Starg_S, 3);
            
            // Branch out from the serialization of this value.
            var branchOut = il.DefineLabel();
            il.Emit(OpCodes.Br_S, branchOut);
            
            // Mask value to be null.
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldloc, locals[LocalBitFieldSerializer]);
            il.Emit(OpCodes.Ldc_I4, nullableFieldIndex++);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            il.MarkLabel(branchOut);
        }

        /// <summary>
        /// Emits instructions for serializing single field.
        ///
        /// Translates roughly to:
        ///     serializer.Serialize(actual.[op-field-name], buffer, offset);
        /// </summary>
        public void EmitSerializeField(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreNextSerializer(serializationValueIndex);
         
            var il = dynamicMethod.GetILGenerator();

            // Push 'actual' to stack from local.
            il.Emit(OpCodes.Ldloc, locals[LocalActual]); 
            // Push serialization value to stack.
            il.Emit(OpCodes.Ldfld, op.Value.Field);                                                       
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                                          
            // Push 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_2);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                                       
            // Call serialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);
            
            EmitIncrementFieldSerializationOffset(op, serializationValueIndex, serializationValueCount);
        }
        
        public void EmitSerializeNonValueTypeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            throw new NotImplementedException();
        }
        
        public void EmitSerializeNullableValueTypeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Emits instructions for serializing single property.
        ///
        /// Translates roughly to:
        ///     serializer.Serialize(actual.[op-property-name], buffer, offset);
        /// </summary>
        public void EmitSerializeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreNextSerializer(serializationValueIndex);
            
            var il = dynamicMethod.GetILGenerator();
            
            // Push 'actual' to stack from local.
            il.Emit(OpCodes.Ldloc, locals[LocalActual]); 
            // Push serialization value to stack.
            il.Emit(OpCodes.Callvirt, op.Value.Property.GetMethod);                                                       
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                                          
            // Push 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_2);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                                       
            // Call serialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);
            
            EmitIncrementPropertySerializationOffset(op, serializationValueIndex, serializationValueCount);
        }
        
        /// <summary>
        /// Emits instructions that declares initial locals for serialization.
        ///
        /// This roughly translates to:
        ///     var actual      = ([program-type])value;
        ///     var serializers = context.ValueSerializers;
        ///     var serializer  = default(ValueSerializer);
        ///     var isNull      = false;
        /// </summary>
        public void EmitLocals(int nullableValuesCount)
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            locals[LocalActual] = il.DeclareLocal(serializationType);                                     
            // Local 1: serializers.
            locals[LocalSerializers] = il.DeclareLocal(typeof(IReadOnlyList<IValueSerializer>)); 
            // Local 2: local for storing the current serializer.
            locals[LocalCurrentSerializer] = il.DeclareLocal(typeof(IValueSerializer));                               
            
            // Cast value to actual value, push argument 'value' to stack.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(serializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox, serializationType);
            il.Emit(OpCodes.Stloc_S, locals[LocalActual]);                                                        
            
            // Get serializers to local.
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.ValueSerializers))!.GetMethod);
            il.Emit(OpCodes.Stloc_S, locals[LocalSerializers]);
            
            // Declare locals for null checks and masking if any of exist.
            if (nullableValuesCount == 0) return;
            
            // Local 3: local for checking null values.
            locals[LocalIsNull] = il.DeclareLocal(typeof(bool));
            // Local 4: for masking null bit fields.
            locals[LocalNullMask] = il.DeclareLocal(typeof(BitField));
            // Local 5: bit field serializer for serializing bit fields.
            locals[LocalBitFieldSerializer] = il.DeclareLocal(typeof(IValueSerializer));
            // Local 6: null mask offset in the buffer.
            locals[LocalNullMaskOffset] = il.DeclareLocal(typeof(int));
                
            // Determine how big of a bit field we need and instantiate bit field local.
            var moduloBitsInBitField = (nullableValuesCount % 8);

            // Add one additional byte to the field if we have any bits that don't fill all bytes.
            var bytesInBitField = (nullableValuesCount / 8) + (moduloBitsInBitField != 0 ? 1 : 0);
                
            il.Emit(OpCodes.Ldloc, locals[LocalNullMask]);
            il.Emit(OpCodes.Ldc_I4, bytesInBitField);
            il.Emit(OpCodes.Call, typeof(BitField).GetConstructor(new [] { typeof(int) })!);
            il.Emit(OpCodes.Stloc_S, locals[LocalNullMask]);
                
            // Store bit field serializer to local.
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.BitFieldSerializer))!.GetMethod);
            il.Emit(OpCodes.Stloc_S, locals[LocalBitFieldSerializer]);
            
            // Store current offset to local 'nullMaskOffset'.
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Stloc_S, locals[LocalNullMaskOffset]);

            // Advance offset by the size of the bit field to leave space for it in front of the buffer.
            il.Emit(OpCodes.Ldarg_3);                                                          
            // Push 'serializer' to stack.
            il.Emit(OpCodes.Ldloc_S, locals[LocalBitFieldSerializer]);                                                          
            // Push 'nullMask' to stack.
            il.Emit(OpCodes.Stloc_S, locals[LocalNullMask]);
            // Box 'nullMask'.
            il.Emit(OpCodes.Box, typeof(BitField));
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
        }
        
        /// <summary>
        /// Attempts to build the dynamic serialize delegate based on instructions received. Throws in case building the dynamic method fails.
        /// </summary>
        public DynamicSerializeDelegate Build()
        {
            var il = dynamicMethod.GetILGenerator();
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                return (DynamicSerializeDelegate)dynamicMethod.CreateDelegate(typeof(DynamicSerializeDelegate));
            } 
            catch (Exception e)
            {
                log.Error(e, $"error occurred while building {nameof(DynamicSerializeDelegate)}");
                
                throw;
            }
        }
    }
    
    public readonly struct ObjectSerializerNullMaskContext
    {
        
    }
    
    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        #endregion

        public static DynamicDeserializeDelegate InterpretDynamicDeserializeDelegate(Type type, 
                                                                                     IReadOnlyList<ISerializationOp> ops, 
                                                                                     int nullableFieldsCount)
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
        
        public static DynamicGetSizeFromValueDelegate InterpretDynamicGetSizeFromValueDelegate(Type type, 
                                                                                               IReadOnlyList<ISerializationOp> ops, 
                                                                                               int nullableFieldsCount)
        {
            return delegate
            {
                return 0;
            };
        }

        public static DynamicSerializeDelegate InterpretDynamicSerializeDelegate(Type type, 
                                                                                 IReadOnlyList<ISerializationOp> ops, 
                                                                                 int nullableFieldsCount)
        {
            var builder = new DynamicSerializeDelegateBuilder(type);
 
            // Declare locals.
            builder.EmitLocals(nullableFieldsCount);
            
            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var serializationValueIndex = 0; serializationValueIndex < ops.Count; serializationValueIndex++)
            {
                var op = ops[serializationValueIndex];

                switch (op)
                {
                    case SerializationFieldOp sfop:
                        if (!sfop.Value.Type.IsValueType) 
                            builder.EmitSerializeNonValueTypeField(sfop, serializationValueIndex, ops.Count);
                        else if (sfop.Value.Type.IsGenericType && sfop.Value.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            builder.EmitSerializeNullableValueTypeField(sfop, serializationValueIndex, ops.Count);
                        else
                            builder.EmitSerializeField(sfop, serializationValueIndex, ops.Count);
                        break;
                    case SerializationPropertyOp spop:
                        if (!spop.Value.Type.IsValueType) 
                            builder.EmitSerializeNonValueTypeProperty(spop, serializationValueIndex, ops.Count);
                        else if (spop.Value.Type.IsGenericType && spop.Value.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                            builder.EmitSerializeNullableValueTypeProperty(spop, serializationValueIndex, ops.Count);
                        else
                            builder.EmitSerializeProperty(spop, serializationValueIndex, ops.Count);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected op code encountered while interpreting dynamic object serializer for type " +
                                                            $"{type.Name}: {op}");
                }
            }
            
            return builder.Build();
        }
        
        public static ObjectSerializationContext CreateObjectSerializationContext(IReadOnlyList<ISerializationOp> ops, IReadOnlyList<IValueSerializer> serializers)
        {
            var nullableValuesCount  = 0;
            var nullableValuesOffset = -1;
            
            for (var i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                
                var serializationType = op switch
                {
                    SerializationFieldOp sfop    => sfop.Value.Type,
                    SerializationPropertyOp spop => spop.Value.Type,
                    _                            => null
                };
                
                if (serializationType == null)
                    continue;

                if (serializationType.IsValueType) 
                    continue;
                
                if (!serializationType.IsGenericType)
                    continue;
                
                if (serializationType.GetGenericTypeDefinition() != typeof(Nullable<>))
                    continue;
                
                if (nullableValuesOffset < 0) 
                    nullableValuesOffset = i;
                    
                nullableValuesCount++;
            }
            
            return new ObjectSerializationContext(serializers, 
                                                  nullableValuesCount, 
                                                  nullableValuesOffset,
                                                  nullableValuesCount != 0 ? ValueSerializerRegistry.GetValueSerializerForType(typeof(BitField)) : null);
        }

        public static ObjectSerializer InterpretSerializer(in ObjectSerializerProgram program)
        {
            // Create context based on program. Instructions from serialize program should be usable when interpreting this function.
            var objectSerializationContext = CreateObjectSerializationContext(program.SerializationOps, program.Serializers);
            
            // Create dynamic serialization function.
            var dynamicSerializeDelegate = InterpretDynamicSerializeDelegate(program.Type, 
                                                                             program.SerializationOps, 
                                                                             objectSerializationContext.NullableValuesCount);
            
            // Create dynamic deserialization function.
            var dynamicDeserializeDelegate = InterpretDynamicDeserializeDelegate(program.Type, 
                                                                                 program.DeserializationOps, 
                                                                                 objectSerializationContext.NullableValuesCount);
            
            // Create dynamic get size function. Instructions from serialize program should be usable when interpreting this function.
            var dynamicGetSizeFromValueDelegate = InterpretDynamicGetSizeFromValueDelegate(program.Type, 
                                                                                           program.SerializationOps, 
                                                                                           objectSerializationContext.NullableValuesCount);

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