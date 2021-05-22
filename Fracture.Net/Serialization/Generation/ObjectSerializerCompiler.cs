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
            var dynamicSerializeDelegate = CompileDynamicSerializeDelegate(serializeProgram);
            
            // Create dynamic deserialization function.
            var dynamicDeserializeDelegate = CompileDynamicDeserializeDelegate(deserializeProgram);
            
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
            
            // Serialize x, y.
            var serializer = objectSerializationContext.ValueSerializers[0];
            serializer.Serialize(actual.X, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.X);
            
            serializer = objectSerializationContext.ValueSerializers[1];
            serializer.Serialize(actual.Y, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.Y);

            // Serialize i, j.
            serializer = objectSerializationContext.ValueSerializers[2];
            serializer.Serialize(actual.I, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.I);
            
            serializer = objectSerializationContext.ValueSerializers[3];
            serializer.Serialize(actual.J, buffer, offset);
            offset += serializer.GetSizeFromValue(actual.J);

            // Serialize inner.
            serializer = objectSerializationContext.ValueSerializers[4];
            
            if (actual.Inner == null)
            {
                objectSerializationContext.NullSerializer.Serialize(null, buffer, offset);   
            }
            else
            {
                serializer.Serialize(actual.Inner, buffer, offset);     
            }
        }

        private static DynamicDeserializeDelegate CompileDynamicDeserializeDelegate(in ObjectSerializationProgram program)
        {
            var builder = new DynamicMethod($"Deserialize{program.Type.Name}", 
                                            typeof(object), 
                                            new [] { typeof(ObjectSerializationContext), typeof(byte[]), typeof(int) }, 
                                            true);
            
            var il = builder.GetILGenerator();
            
            return (DynamicDeserializeDelegate)builder.CreateDelegate(typeof(DynamicDeserializeDelegate));
        }

        private static DynamicSerializeDelegate CompileDynamicSerializeDelegate(in ObjectSerializationProgram program)
        {
            var builder = new DynamicMethod($"Serialize{program.Type.Name}", 
                                            typeof(void), 
                                            new [] { typeof(ObjectSerializationContext), typeof(object), typeof(byte[]), typeof(int) }, 
                                            true);
            
            var il = builder.GetILGenerator();
            
            // Declare locals.
            var localType       = il.DeclareLocal(program.Type);            // Type we are serializing.
            var localSerializer = il.DeclareLocal(typeof(ValueSerializer)); // Local for storing the current serializer.
            var localV2         = il.DeclareLocal(typeof(bool));            // For possible null checks.
            
            // Cast value to actual value.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(program.Type.IsClass ? OpCodes.Castclass : OpCodes.Unbox, program.Type);
            il.Emit(OpCodes.Stloc_0);
            
            // Load value serializers to local and get initial serializer.
            il.Emit(OpCodes.Ldarg_S, 0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty("ValueSerializers")!.GetMethod);

            // Start serialization. Keep track of the emitted field count using for loop to calculate offsets correctly.
            for (var i = 0; i < program.Ops.Count; i++)
            {
                var op = program.Ops[i];

                // Check if the value can be null and serialize null if the value is null.
                
                // Get next serializer.
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<ValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
                il.Emit(OpCodes.Stloc_1);

                switch (op)
                {
                    case SerializationFieldOp sfop:
                        // Emit serialize field.
                        il.Emit(OpCodes.Ldloc_1);
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Ldfld, sfop.Value.Field);
                        il.Emit(OpCodes.Box, sfop.Value.Type);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldarg_3);
                        il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod("Serialize")!);
                        
                        if (i + 1 < program.Ops.Count)
                        {
                            // Add last value serialized to the offset.
                            il.Emit(OpCodes.Ldarg_3);
                            il.Emit(OpCodes.Ldloc_1);
                            il.Emit(OpCodes.Ldloc_0);
                            il.Emit(OpCodes.Ldfld, sfop.Value.Field);
                            il.Emit(OpCodes.Box, sfop.Value.Type);
                            il.Emit(OpCodes.Callvirt, typeof(ValueSerializer).GetMethod("GetSizeFromValue")!);
                            il.Emit(OpCodes.Add);
                            il.Emit(OpCodes.Starg_S, 3);
                            
                            // TODO: continue from IL_003d: starg.s offset - line 49
                        }
                        break;
                    case SerializationPropertyOp spop:
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected op code encountered while interpreting dynamic object serializer for type " +
                                                            $"{program.Type.Name}: {op.ToString()}");
                }
            }

            return (DynamicSerializeDelegate)builder.CreateDelegate(typeof(DynamicSerializeDelegate));
        }
    }
}