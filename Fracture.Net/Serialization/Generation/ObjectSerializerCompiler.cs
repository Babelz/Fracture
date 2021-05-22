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

        public IReadOnlyCollection<ValueSerializer> ValueSerializers
        {
            get;
        }
        #endregion

        public ObjectSerializationContext(ValueSerializer nullSerializer, 
                                          IReadOnlyCollection<ValueSerializer> valueSerializers)
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
    /// Enumeration defining operation codes for the serialization compiler.
    /// </summary>
    public enum SerializationOpCode : byte
    {
        DefaultActivation = 0,
        ParametrizedActivation,
        SerializeField,
        SerializeProperty
    }
    
    public interface ISerializationOp
    {
        #region Properties
        public SerializationOpCode Code
        {
            get;
        }
        #endregion
    }
    
    public readonly struct SerializationParametrizedActivationOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode Code => SerializationOpCode.ParametrizedActivation;
        
        public ConstructorInfo Constructor
        {
            get;
        }

        public IReadOnlyCollection<SerializationValue> Parameters
        {
            get;
        }
        #endregion

        public SerializationParametrizedActivationOp(ConstructorInfo constructor, IReadOnlyCollection<SerializationValue> parameters)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Parameters  = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
        
        public override string ToString()
            => $"{{ op: {Code}, ctor: parametrized, params: {Parameters.Count} }}";
    }
    
    public readonly struct SerializationDefaultActivationOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode Code => SerializationOpCode.DefaultActivation;
        
        public ConstructorInfo Constructor
        {
            get;
        }
        #endregion

        public SerializationDefaultActivationOp(ConstructorInfo constructor)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        }

        public override string ToString()
            => $"{{ op: {Code}, ctor: default }}";
    }

    public readonly struct SerializationFieldOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode Code => SerializationOpCode.SerializeField;
        
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
            => $"{{ op: {Code}, prop: {Value.Name} }}";
    }

    public readonly struct SerializationPropertyOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode Code => SerializationOpCode.SerializeProperty;
        
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
            => $"{{ op: {Code}, prop: {Value.Name} }}";
    }
    
    public readonly struct ObjectSerializationProgram
    {
        #region Properties
        public Type Type
        {
            get;
        }
        
        public IReadOnlyCollection<ISerializationOp> Ops
        {
            get;
        }
        #endregion

        public ObjectSerializationProgram(Type type, IReadOnlyCollection<ISerializationOp> ops)
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
                ops.Add(new SerializationParametrizedActivationOp(mapping.Activator.Constructor, mapping.Activator.Values));
            else
                ops.Add(new SerializationDefaultActivationOp(mapping.Activator.Constructor));

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
        
        private static IReadOnlyCollection<ValueSerializer> GetProgramSerializers(ObjectSerializationProgram program)
        {
            var serializers = new List<ValueSerializer>();
            
            foreach (var op in program.Ops)
            {
                switch (op.Code)
                {
                    case SerializationOpCode.ParametrizedActivation:
                        serializers.AddRange(
                            ((SerializationParametrizedActivationOp)op).Parameters.Select(p => ValueSerializerRegistry.GetValueSerializerForRunType(p.Type))
                        );
                        break;
                    case SerializationOpCode.SerializeField:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForRunType(((SerializationFieldOp)op).Value.Type));
                        break;
                    case SerializationOpCode.SerializeProperty:
                        serializers.Add(ValueSerializerRegistry.GetValueSerializerForRunType(((SerializationPropertyOp)op).Value.Type));
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
            
            return (DynamicSerializeDelegate)builder.CreateDelegate(typeof(DynamicSerializeDelegate));
        }
    }
}