using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

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
    public delegate void DynamicSerializeDelegate(ObjectSerializationContext context, object value, byte[] buffer, int offset);
    
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object DynamicDeserializeDelegate(ObjectSerializationContext context, byte[] buffer, int offset);
    
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
    
    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
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
        
        private static IReadOnlyList<ValueSerializer> GetProgramSerializers(ObjectSerializationProgram program)
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
            public float X;
            public float Y;
            
            public int I;
            public int J;
            
            public Vector2 Inner;
        }
        
        public static void SerializeTestEmit(ObjectSerializationContext objectSerializationContext, object value, byte[] buffer, int offset)
        {
            var actual = (Vector2)value;
            var serializers = objectSerializationContext.ValueSerializers;
            
            // Serialize x, y.
            var serializer = serializers[0];
            serializer.Serialize(actual.X, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.X);
            
            serializer = serializers[1];
            serializer.Serialize(actual.Y, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.Y);

            // Serialize i, j.
            serializer = serializers[2];
            serializer.Serialize(actual.I, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.I);
            
            serializer = serializers[3];
            serializer.Serialize(actual.J, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.J);

            // Serialize inner.
            serializer = serializers[4];
            
            if (actual.Inner == null)
            {
                objectSerializationContext.NullSerializer.Serialize(null, buffer, offset);   
            }
            else
            {
                serializer.Serialize(actual.Inner, buffer, offset);     
            }
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
            var builder = new DynamicMethod($"Serialize{program.Type.Name}", 
                                            typeof(void), 
                                            new []
                                            {
                                                typeof(ObjectSerializationContext), // Argument 0.
                                                typeof(object),                     // Argument 1.
                                                typeof(byte[]),                     // Argument 2.
                                                typeof(int)                         // Argument 3.
                                            }, 
                                            true);
            
            var il = builder.GetILGenerator();
            
            // Declare locals.
            il.DeclareLocal(program.Type);                           // Local 0: type we are serializing.
            il.DeclareLocal(typeof(IReadOnlyList<ValueSerializer>)); // Local 1: serializers.
            il.DeclareLocal(typeof(ValueSerializer));                // Local 2: local for storing the current serializer.
            il.DeclareLocal(typeof(bool));                           // Local 3: for possible null checks.
            
            // Cast value to actual value.
            il.Emit(OpCodes.Ldarg_1);                                                        // Push argument 'value' to stack.
            il.Emit(program.Type.IsClass ? OpCodes.Castclass : OpCodes.Unbox, program.Type); // Cast or unbox top of stack.
            il.Emit(OpCodes.Stloc_0);                                                        // Store cast results from stack to local variable at index 0.
            
            // Get serializers to local.
            il.Emit(OpCodes.Ldarg_0);                                                                          
            // Push serializers to stack.
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.ValueSerializers))!.GetMethod); 
            // Store serializers to local.
            il.Emit(OpCodes.Stloc_1);
            
            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var i = 0; i < program.Ops.Count; i++)
            {
                var op = program.Ops[i];

                // TODO: Check if the value can be null and serialize null if the value is null.
                
                // Load local 'serializers' to stack.
                il.Emit(OpCodes.Ldloc_1);
                // Get current value serializer, push current serializer index to stack.
                il.Emit(OpCodes.Ldc_I4, i);
                // Push serializer at index to stack.
                il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<ValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
                // Store current serializer to local.
                il.Emit(OpCodes.Stloc_2); 

                switch (op)
                {
                    case SerializationFieldOp sfop:
                        // Serialize field, push 'serializer' to stack from local.
                        il.Emit(OpCodes.Ldloc_2);    
                        // Push 'actual' to stack from local.
                        il.Emit(OpCodes.Ldloc_0);                                                                       
                        // Push serialization value to stack.
                        il.Emit(OpCodes.Ldfld, sfop.Value.Field);                                                       
                        // Box serialization value.
                        il.Emit(OpCodes.Box, sfop.Value.Type);                                                          
                        // Push 'buffer' to stack.
                        il.Emit(OpCodes.Ldarg_2);                                                                       
                        // Push 'offset' to stack.
                        il.Emit(OpCodes.Ldarg_3);                                                                       
                        // Call serialize.
                        il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.Serialize))!); 
                        
                        if (i + 1 < program.Ops.Count)
                        {
                            // Add last value serialized to the offset, push 'offset' to stack.
                            il.Emit(OpCodes.Ldarg_3);                                                          
                            // Push 'serializer' to stack.
                            il.Emit(OpCodes.Ldloc_2);                                                          
                            // Push 'actual' to stack.
                            il.Emit(OpCodes.Ldloc_0);                                                          
                            // Push last serialization value to stack.
                            il.Emit(OpCodes.Ldfld, sfop.Value.Field);                                          
                            // Box serialization value.
                            il.Emit(OpCodes.Box, sfop.Value.Type);                                             
                            // Call 'GetSizeFromValue', push size to stack.
                            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod(nameof(ValueSerializer.GetSizeFromValue))!); 
                            // Add offset + size.
                            il.Emit(OpCodes.Add);                                                              
                            // Store current offset to argument 'offset'.
                            il.Emit(OpCodes.Starg_S, 3);                                                       
                        }
                        break;
                    case SerializationPropertyOp spop:
                        // TODO: handle properties.
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected op code encountered while interpreting dynamic object serializer for type " +
                                                            $"{program.Type.Name}: {op}");
                }
            }
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                return (DynamicSerializeDelegate)builder.CreateDelegate(typeof(DynamicSerializeDelegate));
            } 
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}