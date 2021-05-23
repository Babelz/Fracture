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
    public readonly struct ObjectSerializationContext
    {
        #region Properties
        public ValueSerializer NullSerializer
        {
            get;
        }

        public IReadOnlyList<ValueSerializer> ValueSerializers
        {
            get;
        }
        #endregion

        public ObjectSerializationContext(ValueSerializer nullSerializer, 
                                          IReadOnlyList<ValueSerializer> valueSerializers)
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
    
    public readonly struct ObjectSerializerContext
    {
        #region Properties
        public ObjectSerializationContext Context
        {
            get;
        }
        
        public DynamicSerializeDelegate Serialize
        {
            get;
        }
        public DynamicDeserializeDelegate Deserialize
        {
            get;
        }
        #endregion

        public ObjectSerializerContext(in ObjectSerializationContext context, 
                                       DynamicSerializeDelegate serialize,
                                       DynamicDeserializeDelegate deserialize)
        {
            Context     = context;
            Serialize   = serialize ?? throw new ArgumentNullException(nameof(serialize));
            Deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }
    }
    
    /// <summary>
    /// Interface for marking structures to represent serialization operations. 
    /// </summary>
    public interface ISerializationOp
    {
        // Marker interface, nothing to implement.
    }
    
    public readonly struct ParametrizedActivationOp : ISerializationOp
    {
        #region Properties
        public ConstructorInfo Constructor
        {
            get;
        }

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
            => $"{{ op: activate.parametrized, ctor: parametrized, params: {Parameters.Count} }}";
    }

    public readonly struct DefaultActivationOp : ISerializationOp
    {
        #region Properties
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
            => $"{{ op: activation.default, ctor: default }}";
    }

    public readonly struct SerializationFieldOp : ISerializationOp
    {
        #region Properties
        public SerializationValue Value
        {
            get;
        }
        #endregion

        public SerializationFieldOp(SerializationValue value)
        {
            Value = value;
            
            if (!Value.IsField)
                throw new InvalidEnumArgumentException("excepting field serialization value"); 
        }

        public override string ToString()
            => $"{{ op: serialize.field, prop: {Value.Name} }}";
    }

    public readonly struct SerializationPropertyOp : ISerializationOp
    {
        #region Properties
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
            => $"{{ op: serialize.property, prop: {Value.Name} }}";
    }

    public readonly struct ObjectSerializationProgram
    {
        #region Properties
        public Type Type
        {
            get;
        }
        
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
    }
    
    /// <summary>
    /// Static class that translates serialization run types to their serialization operation codes and outputs instructions how the type can be
    /// serialized and deserialized. 
    /// </summary>
    public static class ObjectSerializerCompiler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializationProgram CompileDeserialize(ObjectSerializationMapping mapping)
        {
            var ops = new List<ISerializationOp>();
            
            if (!mapping.Activator.IsDefaultConstructor)
                ops.Add(new ParametrizedActivationOp(mapping.Activator.Constructor, mapping.Activator.Values));
            else
                ops.Add(new DefaultActivationOp(mapping.Activator.Constructor));

            ops.AddRange(mapping.Values.Select(v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)));

            return new ObjectSerializationProgram(mapping.Type, ops.AsReadOnly());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectSerializationProgram CompileSerialize(ObjectSerializationMapping mapping)
        {
            var values = mapping.Activator.IsDefaultConstructor ? mapping.Values : mapping.Activator.Values.Concat(mapping.Values);
            var ops    = values.Select(v => v.IsField ? (ISerializationOp)new SerializationFieldOp(v) : new SerializationPropertyOp(v)).ToList();
            
            return new ObjectSerializationProgram(mapping.Type, ops.AsReadOnly());
        }
    }

    public sealed class ObjectSerializerMethodBuilder
    {
        #region Static fields
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private readonly Dictionary<Type, LocalBuilder> locals;
        private readonly DynamicMethod dynamicMethod;
        private readonly Type serializationType;
        #endregion

        public ObjectSerializerMethodBuilder(DynamicMethod dynamicMethod, Type serializationType)
        {
            this.dynamicMethod     = dynamicMethod ?? throw new ArgumentNullException(nameof(dynamicMethod));
            this.serializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
            
            locals = new Dictionary<Type, LocalBuilder>();
        }
        
        private void EmitStoreNextSerializerToLocal(int serializationValueIndex)
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Load local 'serializers' to stack.
            il.Emit(OpCodes.Ldloc, locals[typeof(IReadOnlyList<ValueSerializer>)]);
            // Get current value serializer, push current serializer index to stack.
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex);
            // Push serializer at index to stack.
            il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<ValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
            // Store current serializer to local.
            il.Emit(OpCodes.Stloc, locals[typeof(ValueSerializer)]); 
            // Serialize field, push 'serializer' to stack from local.
            il.Emit(OpCodes.Ldloc, locals[typeof(ValueSerializer)]);
        }
        
        private void EmitSerializeFieldIncrementOffset(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            if (serializationValueIndex + 1 >= serializationValueCount) return;
            
            var il = dynamicMethod.GetILGenerator();
            
            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                          
            // Push 'serializer' to stack.
            il.Emit(OpCodes.Ldloc, locals[typeof(ValueSerializer)]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);                                                          
            // Push last serialization value to stack.
            il.Emit(OpCodes.Ldfld, op.Value.Field);                                          
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                             
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.GetSizeFromValue))!); 
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
            EmitStoreNextSerializerToLocal(serializationValueIndex);
            
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
            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.Serialize))!);
        }

        public void EmitSerializeField(SerializationFieldOp op, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreNextSerializerToLocal(serializationValueIndex);
         
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
            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.Serialize))!);
            
            EmitSerializeFieldIncrementOffset(op, serializationValueIndex, serializationValueCount);
        }
        
        public void EmitSerializeNonValueTypeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            throw new NotImplementedException();
        }
        
        public void EmitSerializeNullableValueTypeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            throw new NotImplementedException();
        }

        public void EmitSerializeProperty(SerializationPropertyOp op, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreNextSerializerToLocal(serializationValueIndex);
            
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
            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.Serialize))!); 
            
            if (serializationValueIndex + 1 >= serializationValueCount) return;
            
            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                          
            // Push 'serializer' to stack.
            il.Emit(OpCodes.Ldloc, locals[typeof(ValueSerializer)]);                                                          
            // Push 'actual' to stack.
            il.Emit(OpCodes.Ldloc, locals[serializationType]);                                                          
            // Push last serialization value to stack.
            il.Emit(OpCodes.Callvirt, op.Value.Property.GetMethod);                                          
            // Box serialization value.
            il.Emit(OpCodes.Box, op.Value.Type);                                             
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.GetSizeFromValue))!); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
        }
        
        public void EmitSerializationLocals()
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            locals.Add(serializationType, il.DeclareLocal(serializationType));                                     
            // Local 1: serializers.
            locals.Add(typeof(IReadOnlyList<ValueSerializer>), il.DeclareLocal(typeof(IReadOnlyList<ValueSerializer>))); 
            // Local 2: local for storing the current serializer.
            locals.Add(typeof(ValueSerializer), il.DeclareLocal(typeof(ValueSerializer)));                               
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
            il.Emit(OpCodes.Stloc, locals[typeof(IReadOnlyList<ValueSerializer>)]);
        }
        
        public T Build<T>() where T : Delegate
        {
            var il = dynamicMethod.GetILGenerator();
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                return (T)dynamicMethod.CreateDelegate(typeof(T));
            } 
            catch (Exception e)
            {
                log.Error(e, "error occurred while interpreting object serialization program");
                
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
        
        private static void AssertSerializationProgramIntegrity(ObjectSerializationProgram serializeProgram, ObjectSerializationProgram deserializeProgram)
        {
            if (serializeProgram.Type != deserializeProgram.Type)
                throw new InvalidOperationException($"serialization program type \"{deserializeProgram.Type.Name}\" and deserialization program type " +
                                                    $"\"{serializeProgram.Type.Name}\" are different");
            
            var serializeProgramSerializers   = GetProgramSerializers(serializeProgram);
            var deserializeProgramSerializers = GetProgramSerializers(deserializeProgram);
            
            if (serializeProgramSerializers.Count != deserializeProgramSerializers.Count) 
                throw new InvalidOperationException($"serialization programs for type \"{serializeProgram.Type.Name}\" have different count of value serializers");
        }

        public static IReadOnlyList<ValueSerializer> GetProgramSerializers(ObjectSerializationProgram program)
        {
            var serializers = new List<ValueSerializer>();
            
            foreach (var op in program.Ops)
            {
                switch (op)
                {
                    case ParametrizedActivationOp paop:
                        serializers.AddRange(
                            paop.Parameters.Select(p => ValueSerializerRegistry.GetValueSerializerForRunType(p.Type))
                        );
                        break;
                    case SerializationFieldOp sfop:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForRunType(sfop.Value.Type));
                        break;
                    case SerializationPropertyOp spop:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForRunType(spop.Value.Type));
                        break;
                    default:
                        continue;
                }
            }
            
            return serializers.AsReadOnly();
        }

        public static ObjectSerializerContext InterpretSerializer(in ObjectSerializationProgram serializeProgram, in ObjectSerializationProgram deserializeProgram)
        {
            // Make sure programs are valid.
            AssertSerializationProgramIntegrity(serializeProgram, deserializeProgram);
            
            // Get serializers for the type.
            var valueSerializers = GetProgramSerializers(serializeProgram);
            
            // Create dynamic serialization function.
            var dynamicSerializeDelegate = InterpretDynamicSerializeDelegate(serializeProgram);
            
            // Create dynamic deserialization function.
            var dynamicDeserializeDelegate = InterpretDynamicDeserializeDelegate(deserializeProgram);
            
            // TODO: generate dynamic SizeFromValue delegate.
            
            return new ObjectSerializerContext(
                new ObjectSerializationContext(ValueSerializerRegistry.GetValueSerializerForRunType(null),
                                               valueSerializers),
                dynamicSerializeDelegate,
                dynamicDeserializeDelegate
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

        public static DynamicSerializeDelegate InterpretDynamicSerializeDelegate(in ObjectSerializationProgram program)
        {
            var builder = new ObjectSerializerMethodBuilder(
                new DynamicMethod($"Serialize{program.Type.Name}", 
                                  typeof(void), 
                                  new []
                                  {
                                      typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                                      typeof(object),                                     // Argument 1.
                                      typeof(byte[]),                                     // Argument 2.
                                      typeof(int)                                         // Argument 3.
                                  }, 
                                  true),
                program.Type);
            
            // Declare locals.
            builder.EmitSerializationLocals();
            
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
            
            return builder.Build<DynamicSerializeDelegate>();
        }
    }
}