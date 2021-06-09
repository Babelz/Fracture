using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Fracture.Net.Serialization.Generation.Delegates
{
    
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationContext context, object value);
    
    public sealed class DynamicGetSizeFromValueDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 
        #endregion
        #region Fields
        private readonly DynamicMethod dynamicMethod;
        
        private readonly Type serializationType;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(Type serializationType)
        {
            this.serializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
         
            dynamicMethod = new DynamicMethod(
                $"GetSizeOf{this.serializationType.Name}Value", 
                typeof(void), 
                new []
                {
                    typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                    typeof(object)                                      // Argument 1.
                },
                true
            );

            var locals = new LocalBuilder[MaxLocals];
            var temp   = new Dictionary<Type, LocalBuilder>();
        }

        public void EmitLocals(int nullableFieldsCount)
        {
            throw new NotImplementedException();
        }

        public void EmitGetSizeOfNonValueTypeValue(in SerializationValue value, int serializationValueIndex, int opsCount)
        {
            throw new NotImplementedException();
        }

        public void EmitGetSizeOfNullableValue(in SerializationValue value, int serializationValueIndex, int opsCount)
        {
            throw new NotImplementedException();
        }

        public void EmitGetSizeOfValue(in SerializationValue value, int serializationValueIndex, int opsCount)
        {
            throw new NotImplementedException();
        }

        public DynamicGetSizeFromValueDelegate Build()
        {
            throw new NotImplementedException();
        }
    }
}