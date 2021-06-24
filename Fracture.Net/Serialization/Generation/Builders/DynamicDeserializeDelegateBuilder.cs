using System;
using System.Reflection;
using System.Reflection.Emit;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object DynamicDeserializeDelegate(in ObjectSerializationContext context, byte[] buffer, int offset);
    
    public sealed class DynamicDeserializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 1;
        #endregion

        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly byte localValue;
        #endregion
        
        public DynamicDeserializeDelegateBuilder(in ObjectSerializationContext context, Type serializationType) 
            : base(in context, 
                   serializationType,
                   new DynamicMethod(
                       $"Deserialize", 
                       typeof(object), 
                       new []
                       {
                           typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                           typeof(byte[]),                                     // Argument 1.
                           typeof(int)                                         // Argument 2.
                       },
                       true
                   ),
                   MaxLocals)
        {
            localValue = AllocateNextLocalIndex();
        }

        public void EmitDeserializeNonValueTypeValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitDeserializeNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitDeserializeValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitActivation(ConstructorInfo constructor)
        {
            throw new NotImplementedException();
        }

        public void EmitLoadNonValueTypeValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitLoadNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitLoadValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public DynamicDeserializeDelegate Build()
        {
            try
            {
                var method = (DynamicDeserializeDelegate)DynamicMethod.CreateDelegate(typeof(DynamicDeserializeDelegate), new object());
                
                return (in ObjectSerializationContext context, byte[] buffer, int offset) =>
                {
                    try
                    {
                        return method(context, buffer, offset);
                    }
                    catch (Exception e)
                    {
                        throw new DynamicDeserializeException(SerializationType, e, buffer, offset);
                    }
                };
            } 
            catch (Exception e)
            {
                Log.Error(e, $"error occurred while building {nameof(DynamicSerializeDelegate)}");
                
                throw;
            }
        }
    }
}