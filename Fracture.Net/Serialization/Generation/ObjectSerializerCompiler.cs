using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

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
    public delegate void Serialize(ObjectSerializationContext context, object value, byte[] buffer, int offset);
    
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object Deserialize(ObjectSerializationContext context, byte[] buffer, int offset);
    
    public readonly struct ObjectSerializerContext
    {
        #region Properties
        public ObjectSerializationContext Context
        {
            get;
        }
        
        public Serialize Serialize
        {
            get;
        }
        public Deserialize Deserialize
        {
            get;
        }
        #endregion

        public ObjectSerializerContext(ObjectSerializationContext context, 
                                       Serialize serialize,
                                       Deserialize deserialize)
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
        SerializeProperty,
        DeserializeField,
        DeserializeProperty
    }
    
    public interface ISerializationOp
    {
        #region Properties
        public SerializationOpCode OpCode
        {
            get;
        }
        #endregion
    }
    
    public readonly struct SerializationParametrizedActivationOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode OpCode => SerializationOpCode.ParametrizedActivation;
        
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
            => $"{{ op: {OpCode}, ctor: parametrized, params: {Parameters.Count} }}";
    }
    
    public readonly struct SerializationDefaultActivationOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode OpCode => SerializationOpCode.DefaultActivation;
        
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
            => $"{{ op: {OpCode}, ctor: default }}";
    }

    public readonly struct SerializationFieldOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode OpCode => SerializationOpCode.SerializeField;
        
        private SerializationValue Value
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
            => $"{{ op: {OpCode}, prop: {Value.Name} }}";
    }

    public readonly struct SerializationPropertyOp : ISerializationOp
    {
        #region Properties
        public SerializationOpCode OpCode => SerializationOpCode.SerializeProperty;
        
        private SerializationValue Value
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
            => $"{{ op: {OpCode}, prop: {Value.Name} }}";
    }
    
    public readonly struct ObjectSerializationProgram
    {
        #region Properties
        private Type Type
        {
            get;
        }
        
        private IReadOnlyCollection<ISerializationOp> Ops
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
        public static ObjectSerializationProgram CompileDeserializer(ObjectSerializationMapping mapping)
        {
            throw new NotImplementedException();
        }
        
        public static ObjectSerializationProgram CompileSerializer(ObjectSerializationMapping mapping)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Static class that translates compiled object serialization instructions to actual serializers.
    /// </summary>
    public static class ObjectSerializerInterpreter
    {
        public static ObjectSerializerContext InterpretDeserialize(ObjectSerializationProgram program)
        {
            throw new NotImplementedException();
        }
        
        public static ObjectSerializerContext InterpretSerialize(ObjectSerializationProgram program)
        {
            throw new NotImplementedException();
        }
    }
}