using System;
using System.Reflection.Emit;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping serialization functions.
    /// </summary>
    public delegate void DynamicSerializeDelegate(object value, byte[] buffer, int offset);
    
    /// <summary>
    /// Class that provides dynamic functions building for dynamic serialization functions. 
    /// </summary>
    public sealed class DynamicSerializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 2;
        #endregion
        
        #region Fields
        private readonly byte localNullMask;
        private readonly byte localNullMaskOffset;
        #endregion
        
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private int nullableFieldIndex;
        #endregion

        public DynamicSerializeDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType)
            : base(valueRanges,
                   serializationType,
                   new DynamicMethod(
                       $"Serialize", 
                       typeof(void), 
                       new []
                       {
                           typeof(object), // Argument 0.
                           typeof(byte[]), // Argument 1.
                           typeof(int)     // Argument 2.
                       },
                       true
                   ),
                   MaxLocals)
        {
            localNullMask       = AllocateNextLocalIndex();
            localNullMaskOffset = AllocateNextLocalIndex();
        }

        /// <summary>
        /// Emits instructions for serializing single value that is not value type.
        ///
        /// Translates roughly to:
        ///     if (field != null) {
        ///        currentSerializer.Serialize(actual.[op-value-name], buffer, offset);
        ///        offset += currentSerializer.GetSizeFromValue(actual.[op-value-name]);
        ///     } else {
        ///        nullMask.SetBit(serializationValueIndex, true);
        ///     }
        /// </summary>
        public void EmitSerializeNonValueTypeValue(SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Push serialization value to stack.
            EmitLoadSerializationValue(il, value);
            
            // Push null to stack.
            il.Emit(OpCodes.Ldnull);
            // Check if serialization value is null.
            il.Emit(OpCodes.Cgt_Un);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            // Push value of the field to stack boxed.
            EmitLoadSerializationValue(il, value);
            
            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));

            if (serializationValueIndex + 1 < ValueRanges.SerializationValuesCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                il.Emit(OpCodes.Ldarg_2);
                
                // Push value from the nullable field to stack boxed.
                EmitLoadSerializationValue(il, value);
                
                il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
                // Advance offset by the size of the value.
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, 2);
            }

            // Branch out from the serialization of this value.
            var branchOut = il.DefineLabel();
            il.Emit(OpCodes.Br_S, branchOut);
            
            // Mask value to be null.
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, nullableFieldIndex - ValueRanges.NullableValuesOffset);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            il.MarkLabel(branchOut);
            
            nullableFieldIndex++;
        }
        
        /// <summary>
        /// Emits instructions for serializing single nullable value.
        ///
        /// Translates roughly to:
        ///     if (field.HasValue) {
        ///        currentSerializer.Serialize(actual.[op-value-name].Value, buffer, offset);
        ///        offset += currentSerializer.GetSizeFromValue(actual.[op-value-name].Value);
        ///     } else {
        ///        nullMask.SetBit(serializationValueIndex, true);
        ///     }
        /// </summary>
        public void EmitSerializeNullableValue(SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();

            // Push serialization value to stack.
            EmitLoadSerializationValueAddressToStack(il, value);
            
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            // Push value from the nullable field to stack boxed.
            EmitLoadSerializationValueAddressToStack(il, value);
            
            il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));
            
            // Push value from the nullable field to stack boxed, increment serialization offset.
            if (serializationValueIndex + 1 < ValueRanges.SerializationValuesCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                il.Emit(OpCodes.Ldarg_2);
                
                EmitLoadSerializationValueAddressToStack(il, value);
                
                il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
                il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
                // Advance offset by the size of the value.
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, 2);
            }
            
            // Branch out from the serialization of this value.
            var setNullBit = il.DefineLabel();
            il.Emit(OpCodes.Br_S, setNullBit);
            
            // Mask value to be null.
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, nullableFieldIndex - ValueRanges.NullableValuesOffset);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            il.MarkLabel(setNullBit);
            
            nullableFieldIndex++;
        }

        /// <summary>
        /// Emits instructions for serializing single value.
        ///
        /// Translates roughly to:
        ///     currentSerializer.Serialize(actual.[op-value-name], buffer, offset);
        ///     offset += currentSerializer.GetSizeFromValue(actual.[op-value-name]);
        /// </summary>
        public void EmitSerializeValue(SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Push serialization value to stack.
            EmitLoadSerializationValue(il, value);
            
            // Push 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_1);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);
            // Call serialize.
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));
            
            if (serializationValueIndex + 1 >= ValueRanges.SerializationValuesCount) return;
            
            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);                    

            // Push last serialization value to stack.
            EmitLoadSerializationValue(il, value); 
                                              
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 2);
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
        public override void EmitLocals()
        {
            base.EmitLocals();

            var il = DynamicMethod.GetILGenerator();

            EmitStoreArgumentValueToLocal(il);
            
            // Declare locals for null checks and masking if any of exist.
            if (ValueRanges.NullableValuesCount == 0) return;
            
            // Local 1: for masking null bit fields.
            Locals[localNullMask] = il.DeclareLocal(typeof(BitField));
            // Local 2: null mask offset in the buffer.
            Locals[localNullMaskOffset] = il.DeclareLocal(typeof(int));

            // Instantiate local 'nullMask' bit field.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, BitField.LengthFromBits(ValueRanges.NullableValuesCount));
            il.Emit(OpCodes.Call, typeof(BitField).GetConstructor(new [] { typeof(int) })!);

            // Store current offset to local 'nullMaskOffset'.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc_S, Locals[localNullMaskOffset]);

            // Advance offset by the size of the bit field to leave space for it in front of the buffer.
            il.Emit(OpCodes.Ldarg_2);                                                            
            // Push local 'nullMask' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localNullMask]);
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(typeof(BitFieldSerializer)));
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 2);
        }
        
        /// <summary>
        /// Attempts to build the dynamic serialize delegate based on instructions received. Throws in case building the dynamic method fails.
        /// </summary>
        public DynamicSerializeDelegate Build()
        {
            var il = DynamicMethod.GetILGenerator();
            
            if (ValueRanges.NullableValuesCount != 0)
            {
                // Push local 'nullMask' to stack.
                il.Emit(OpCodes.Ldloc_S, Locals[localNullMask]);
                // Push argument 'buffer' to stack.
                il.Emit(OpCodes.Ldarg_1);                                                                       
                // Push local 'nullMaskOffset' to stack.
                il.Emit(OpCodes.Ldloc_S, Locals[localNullMaskOffset]);                                                                       
                // Call serialize.
                il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSerializeMethodInfo(typeof(BitFieldSerializer)));
            }
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicSerializeDelegate)DynamicMethod.CreateDelegate(typeof(DynamicSerializeDelegate));
                
                return (value, buffer, offset) =>
                {
                    try
                    {
                        method(value, buffer, offset);
                    }
                    catch (Exception e)
                    {
                        throw new DynamicSerializeException(SerializationType, e, value);
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