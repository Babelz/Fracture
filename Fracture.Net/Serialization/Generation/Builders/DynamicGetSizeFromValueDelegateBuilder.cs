using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationContext context, object value);
    
    public sealed class DynamicGetSizeFromValueDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Fields
        private readonly byte LocalNullMask;
        private readonly byte LocalSize;
        
        private const int MaxLocals = 1;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(Type serializationType)
            : base(serializationType,
                   new DynamicMethod(
                       $"GetSizeFromValue", 
                       typeof(void), 
                       new []
                       {
                           typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                           typeof(object)                                      // Argument 1.
                       },
                       true
                   ),
                   MaxLocals)
        {
            LocalNullMask = AllocateNextLocalIndex();
            LocalSize     = AllocateNextLocalIndex();
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
        
        public override void EmitLocals(int nullableFieldsCount)
        {
            base.EmitLocals(nullableFieldsCount);
            
            var il = DynamicMethod.GetILGenerator();

            // Local 3: total size of the value.
            Locals[LocalSize] = il.DeclareLocal(typeof(ushort));

            if (nullableFieldsCount == 0) return;
            
            // Local 4: null mask for checking if object values are null.
            Locals[LocalNullMask] = il.DeclareLocal(typeof(BitField));
        }

        public DynamicGetSizeFromValueDelegate Build()
        {
            throw new NotImplementedException();
        }
    }
}