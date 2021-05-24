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
        /// Gets the null serializer for serializing nullable and null values.
        /// </summary>
        public IValueSerializer NullSerializer
        {
            get;
        }

        /// <summary>
        /// Gets the value serializers for the type.
        /// </summary>
        public IReadOnlyList<IValueSerializer> ValueSerializers
        {
            get;
        }
        #endregion

        public ObjectSerializationContext(IValueSerializer nullSerializer, 
                                          IReadOnlyList<IValueSerializer> valueSerializers)
        {
            NullSerializer   = nullSerializer ?? throw new ArgumentNullException(nameof(nullSerializer));
            ValueSerializers = valueSerializers ?? throw new ArgumentNullException(nameof(valueSerializers));
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
    /// Class that wraps dynamic serialization for single type and provides serialization.
    /// </summary>
    public sealed class DynamicObjectSerializer 
    {
        #region Fields
        private readonly ObjectSerializationContext context; 
        private readonly DynamicSerializeDelegate serialize;
        private readonly DynamicDeserializeDelegate deserialize;
        private readonly DynamicGetSizeFromValueDelegate getSizeFromValue;
        #endregion

        public DynamicObjectSerializer(in ObjectSerializationContext context, 
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
    /// Structure that represents a serialization program for specific type. Information used in this structure can be used for generating dynamic serializers
    /// for objects. 
    /// </summary>
    public readonly struct ObjectSerializationProgram
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
        /// Gets the instruction list for this program. Dynamic serializers are generated based on these instructions. 
        /// </summary>
        public IReadOnlyList<ISerializationOp> Ops
        {
            get;
        }
        #endregion

        public ObjectSerializationProgram(Type type, IReadOnlyList<ISerializationOp> ops)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Ops  = ops ?? throw new ArgumentNullException(nameof(ops));
        }
        
        /// <summary>
        /// Returns list containing all value serializers that this program uses.
        /// </summary>
        public IReadOnlyList<IValueSerializer> GetProgramSerializers()
        {
            var serializers = new List<IValueSerializer>();
            
            foreach (var op in Ops)
            {
                switch (op)
                {
                    case ParametrizedActivationOp paop:
                        serializers.AddRange(
                            paop.Parameters.Select(p => ValueSerializerRegistry.GetValueSerializerForType(p.Type))
                        );
                        break;
                    case SerializationFieldOp sfop:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForType(sfop.Value.Type));
                        break;
                    case SerializationPropertyOp spop:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForType(spop.Value.Type));
                        break;
                    default:
                        continue;
                }
            }
            
            return serializers.AsReadOnly();
        }
    }
    
    /// <summary>
    /// Structure that represents full serialization program. This structure contains instructions for generating both the serialize and deserialize functions.
    /// </summary>
    public readonly struct DynamicObjectSerializerProgram
    {
        #region Properties
        /// <summary>
        /// Gets the program for generating serialize function.
        /// </summary>
        public ObjectSerializationProgram SerializeProgram
        {
            get;
        }

        /// <summary>
        /// Gets the program for generating deserialization function.
        /// </summary>
        public ObjectSerializationProgram DeserializeProgram
        {
            get;
        }
        #endregion

        public DynamicObjectSerializerProgram(in ObjectSerializationProgram serializeProgram, in ObjectSerializationProgram deserializeProgram)
        {
            SerializeProgram   = serializeProgram;
            DeserializeProgram = deserializeProgram;
            
            if (serializeProgram.Type != deserializeProgram.Type)
                throw new InvalidOperationException($"serialization program type \"{deserializeProgram.Type.Name}\" and deserialization program type " +
                                                    $"\"{serializeProgram.Type.Name}\" are different");
            
            var serializeProgramSerializers   = serializeProgram.GetProgramSerializers();
            var deserializeProgramSerializers = deserializeProgram.GetProgramSerializers();
            
            if (serializeProgramSerializers.Count != deserializeProgramSerializers.Count) 
                throw new InvalidOperationException($"serialization programs for type \"{serializeProgram.Type.Name}\" have different count of value serializers");
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
        public static ObjectSerializationProgram CompileDeserializeProgram(in ObjectSerializationMapping mapping)
        {
            var ops = new List<ISerializationOp>();
            
            if (!mapping.Activator.IsDefaultConstructor)
                ops.Add(new ParametrizedActivationOp(mapping.Activator.Constructor, mapping.Activator.Values));
            else
                ops.Add(new DefaultActivationOp(mapping.Activator.Constructor));

            ops.AddRange(mapping.Values.Select(v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)));

            return new ObjectSerializationProgram(mapping.Type, ops.AsReadOnly());
        }
        
        /// <summary>
        /// Compiles serialization instructions from given mappings to <see cref="ObjectSerializationProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializationProgram CompileSerializeProgram(in ObjectSerializationMapping mapping)
        {
            var values = mapping.Activator.IsDefaultConstructor ? mapping.Values : mapping.Activator.Values.Concat(mapping.Values);
            var ops    = values.Select(v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)).ToList();
            
            return new ObjectSerializationProgram(mapping.Type, ops.AsReadOnly());
        }
        
        /// <summary>
        /// Compiles both serialization and deserialization instructions from given mappings to <see cref="DynamicObjectSerializerProgram"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicObjectSerializerProgram CompileSerializerProgram(ObjectSerializationMapping mapping)
            => new DynamicObjectSerializerProgram(CompileSerializeProgram(mapping), CompileDeserializeProgram(mapping));
    }

    /// <summary>
    /// Class that provides dynamic functions building for dynamic serialization functions. 
    /// </summary>
    public sealed class DynamicSerializeDelegateBuilder
    {
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly Dictionary<Type, LocalBuilder> locals;
        private readonly DynamicMethod dynamicMethod;
        private readonly Type serializationType;
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
            
            locals = new Dictionary<Type, LocalBuilder>();
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
            il.Emit(OpCodes.Ldloc, locals[typeof(IReadOnlyList<IValueSerializer>)]);
            // Get current value serializer, push current serializer index to stack.
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex);
            // Push serializer at index to stack.
            il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<IValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
            // Store current serializer to local.
            il.Emit(OpCodes.Stloc, locals[typeof(IValueSerializer)]); 
            // Serialize field, push 'serializer' to stack from local.
            il.Emit(OpCodes.Ldloc, locals[typeof(IValueSerializer)]);
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
            il.Emit(OpCodes.Ldloc, locals[typeof(IValueSerializer)]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);                                                          
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
            il.Emit(OpCodes.Ldloc, locals[typeof(IValueSerializer)]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);                                                          
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
            
            if (!locals.TryGetValue(op.Value.Field.FieldType, out var localNullable))
            {
                // Declare new local for the new nullable type.
                localNullable = il.DeclareLocal(op.Value.Field.FieldType);
                
                locals.Add(op.Value.Field.FieldType, localNullable);
            }
            
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);
            // Push serialization value to stack.
            il.Emit(OpCodes.Ldfld, op.Value.Field);
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, op.Value.Field.FieldType.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value of the field.
            var hasValue = il.DefineLabel();
            var isNull   = il.DefineLabel();
            
            il.Emit(OpCodes.Brtrue_S, hasValue);
            
            // Branch value is not null, get next serializer.
            il.MarkLabel(hasValue);
            EmitStoreNextSerializer(serializationValueIndex);
            
            // Branch value is null.
            il.MarkLabel(isNull);
            // Load serialization context to stack.
            il.Emit(OpCodes.Ldarg_0);
            // Get null serializer and push it to stack.
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.NullSerializer))!.GetMethod);
            
            // Jump to serialization.
            var serialize = il.DefineLabel();
            
            il.Emit(OpCodes.Br_S, serialize); 
            
            // Do the actual serialization with the serializer we selected in the previous branch.
            il.MarkLabel(serialize);
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);
            // Call property get and push value to stack.
            il.Emit(OpCodes.Callvirt, op.Value.Property.GetMethod);
            // Store return value to local at index.
            il.Emit(OpCodes.Stloc, locals[op.Value.Type]);
            // Push local at index to stack.
            il.Emit(OpCodes.Ldloc_S, locals[op.Value.Type]);
            // Get value from nullable and push it to stack.
            il.Emit(OpCodes.Call, op.Value.Type.GetProperty("Value")!.GetMethod);
            // Box nullable value on stack.
            il.Emit(OpCodes.Box, op.Value.Type.GetGenericArguments()[0]);
            // Push argument 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_2);
            // Push argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);
            // Serialize the value.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);
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
            il.Emit(OpCodes.Ldloc, locals[serializationType]); 
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
            il.Emit(OpCodes.Ldloc, locals[serializationType]); 
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
        public void EmitLocals()
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            locals.Add(serializationType, il.DeclareLocal(serializationType));                                     
            // Local 1: serializers.
            locals.Add(typeof(IReadOnlyList<IValueSerializer>), il.DeclareLocal(typeof(IReadOnlyList<IValueSerializer>))); 
            // Local 2: local for storing the current serializer.
            locals.Add(typeof(IValueSerializer), il.DeclareLocal(typeof(IValueSerializer)));                               
            // Local 3: for possible null checks.
            locals.Add(typeof(bool), il.DeclareLocal(typeof(bool)));                                                     
            
            // Cast value to actual value, push argument 'value' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Cast or unbox top of stack.
            il.Emit(serializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox, serializationType); 
            // Store cast results from stack to local variable at index 0.
            il.Emit(OpCodes.Stloc, locals[serializationType]);                                                        
            
            // Get serializers to local.
            il.Emit(OpCodes.Ldarg_0);                                                                          
            // Push serializers to stack.
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.ValueSerializers))!.GetMethod); 
            // Store serializers to local.
            il.Emit(OpCodes.Stloc, locals[typeof(IReadOnlyList<IValueSerializer>)]);
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
    
    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        #endregion

        public static DynamicObjectSerializer InterpretSerializer(in DynamicObjectSerializerProgram program)
        {
            // Create dynamic serialization function.
            var dynamicSerializeDelegate = InterpretDynamicSerializeDelegate(program.SerializeProgram);
            
            // Create dynamic deserialization function.
            var dynamicDeserializeDelegate = InterpretDynamicDeserializeDelegate(program.DeserializeProgram);
            
            // Create dynamic get size function. Instructions from serialize program should be usable when interpreting this function.
            var dynamicGetSizeFromValueDelegate = InterpretDynamicGetSizeFromValueDelegate(program.SerializeProgram);
            
            return new DynamicObjectSerializer(
                new ObjectSerializationContext(ValueSerializerRegistry.GetValueSerializerForType(null),
                                               program.SerializeProgram.GetProgramSerializers()),
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
            
            // Serialize x, y.
            var serializer = actual.X.HasValue ? serializers[0] : objectSerializationContext.NullSerializer;
            serializer.Serialize(actual.X, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.X);
            
            serializer = actual.Y.HasValue ? serializers[1] : objectSerializationContext.NullSerializer;
            serializer.Serialize(actual.Y, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.Y);
            
            serializer = serializers[2];
            serializer.Serialize(actual.I, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.I);
            
            serializer = serializers[3];
            serializer.Serialize(actual.J, buffer, offset);
            
            // // Serialize i, j.
            // serializer = serializers[2];
            // serializer.Serialize(actual.I, buffer, offset);
            // offset += serializer.GetSizeFromValue(actual.I);
            //
            // serializer = serializers[3];
            // serializer.Serialize(actual.J, buffer, offset);
            // offset += serializer.GetSizeFromValue(actual.J);
            //
            // // Serialize inner.
            // serializer = serializers[4];
            //
            // if (actual.Inner == null)
            // {
            //     objectSerializationContext.NullSerializer.Serialize(null, buffer, offset);   
            // }
            // else
            // {
            //     serializer.Serialize(actual.Inner, buffer, offset);     
            // }
        }
        
        public static DynamicDeserializeDelegate InterpretDynamicDeserializeDelegate(in ObjectSerializationProgram program)
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
        
        public static DynamicGetSizeFromValueDelegate InterpretDynamicGetSizeFromValueDelegate(in ObjectSerializationProgram program)
        {
            return delegate
            {
                return 0;
            };
        }

        public static DynamicSerializeDelegate InterpretDynamicSerializeDelegate(in ObjectSerializationProgram program)
        {
            var builder = new DynamicSerializeDelegateBuilder(program.Type);
            
            // Declare locals.
            builder.EmitLocals();
            
            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var serializationValueIndex = 0; serializationValueIndex < program.Ops.Count; serializationValueIndex++)
            {
                var op = program.Ops[serializationValueIndex];

                switch (op)
                {
                    case SerializationFieldOp sfop:
                        if (!sfop.Value.Type.IsValueType) 
                            builder.EmitSerializeNonValueTypeField(sfop, serializationValueIndex, program.Ops.Count);
                        else if (sfop.Value.Type == typeof(Nullable<>))
                            builder.EmitSerializeNullableValueTypeField(sfop, serializationValueIndex, program.Ops.Count);
                        else
                            builder.EmitSerializeField(sfop, serializationValueIndex, program.Ops.Count);
                        break;
                    case SerializationPropertyOp spop:
                        if (!spop.Value.Type.IsValueType) 
                            builder.EmitSerializeNonValueTypeProperty(spop, serializationValueIndex, program.Ops.Count);
                        else if (spop.Value.Type == typeof(Nullable<>))
                            builder.EmitSerializeNullableValueTypeProperty(spop, serializationValueIndex, program.Ops.Count);
                        else
                            builder.EmitSerializeProperty(spop, serializationValueIndex, program.Ops.Count);
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected op code encountered while interpreting dynamic object serializer for type " +
                                                            $"{program.Type.Name}: {op}");
                }
            }
            
            return builder.Build();
        }
    }
}