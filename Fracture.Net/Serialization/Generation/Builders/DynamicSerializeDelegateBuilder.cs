using System;
using System.Reflection.Emit;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping serialization functions.
    /// </summary>
    public delegate void DynamicSerializeDelegate(in ObjectSerializationContext context, object value, byte[] buffer, int offset);
    
    /// <summary>
    /// Class that provides dynamic functions building for dynamic serialization functions. 
    /// </summary>
    public sealed class DynamicSerializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 3;
        #endregion
        
        #region Fields
        private readonly byte localNullMask;
        private readonly byte localNullMaskOffset;
        private readonly byte localBitFieldSerializer;
        #endregion
        
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion

        #region Fields
        private int nullableFieldIndex;
        #endregion

        public DynamicSerializeDelegateBuilder(Type serializationType)
            : base(serializationType,
                   new DynamicMethod(
                       $"Serialize", 
                       typeof(void), 
                       new []
                       {
                           typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                           typeof(object),                                     // Argument 1.
                           typeof(byte[]),                                     // Argument 2.
                           typeof(int)                                         // Argument 3.
                       },
                       true
                   ),
                   MaxLocals)
        {
            localNullMask           = AllocateNextLocalIndex();
            localNullMaskOffset     = AllocateNextLocalIndex();
            localBitFieldSerializer = AllocateNextLocalIndex();
        }

        /// <summary>
        /// Emits instructions for serializing single value that is not value type.
        ///
        /// Translates roughly to:
        ///     if (field != null) {
        ///        serializer.Serialize(actual.[op-value-name], buffer, offset);
        ///        offset += serializer.GetSizeFromValue(actual.[op-value-name]);
        ///     } else {
        ///        nullMask.SetBit(serializationValueIndex, true);
        ///     }
        /// </summary>
        public void EmitSerializeNonValueTypeValue(SerializationValue value, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            // Push serialization value to stack.
            EmitPushSerializationValueToStack(il, value);
            
            // Push null to stack.
            il.Emit(OpCodes.Ldnull);
            // Check if serialization value is null.
            il.Emit(OpCodes.Cgt_Un);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            // Push current serializer to stack.
            EmitPushCurrentSerializerToStack(il);

            // Push value of the field to stack boxed.
            EmitPushSerializationValueToStack(il, value);
            
            il.Emit(OpCodes.Box, value.Type);
            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);

            if (serializationValueIndex + 1 < serializationValueCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                il.Emit(OpCodes.Ldarg_3);
                
                EmitPushCurrentSerializerToStack(il);
                
                // Push value from the nullable field to stack boxed.
                EmitPushSerializationValueToStack(il, value);
            
                il.Emit(OpCodes.Box, value.Type);
                il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
                // Advance offset by the size of the value.
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, 3);
            }

            // Branch out from the serialization of this value.
            var branchOut = il.DefineLabel();
            il.Emit(OpCodes.Br_S, branchOut);
            
            // Mask value to be null.
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, nullableFieldIndex++);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            il.MarkLabel(branchOut);
        }
        
        /// <summary>
        /// Emits instructions for serializing single nullable value.
        ///
        /// Translates roughly to:
        ///     if (field.HasValue) {
        ///        serializer.Serialize(actual.[op-value-name].Value, buffer, offset);
        ///        offset += serializer.GetSizeFromValue(actual.[op-value-name].Value);
        ///     } else {
        ///        nullMask.SetBit(serializationValueIndex, true);
        ///     }
        /// </summary>
        public void EmitSerializeNullableValue(SerializationValue value, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();

            // Push serialization value to stack.
            EmitPushSerializationValueAddressToStack(il, value);
            
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            // Push current serializer to stack.
            EmitPushCurrentSerializerToStack(il);

            // Push value from the nullable field to stack boxed.
            EmitPushSerializationValueAddressToStack(il, value);
            
            il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
            il.Emit(OpCodes.Box, value.Type.GenericTypeArguments[0]);
            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);

            // Push value from the nullable field to stack boxed, increment serialization offset.
            if (serializationValueIndex + 1 < serializationValueCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                il.Emit(OpCodes.Ldarg_3);
                
                EmitPushSerializationValueAddressToStack(il, value);
                
                EmitPushSerializationValueAddressToStack(il, value);
                
                il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
                il.Emit(OpCodes.Box, value.Type.GenericTypeArguments[0]);
                il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
                // Advance offset by the size of the value.
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Starg_S, 3);
            }
            
            // Branch out from the serialization of this value.
            var setNullBit = il.DefineLabel();
            il.Emit(OpCodes.Br_S, setNullBit);
            
            // Mask value to be null.
            il.MarkLabel(notNull);
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, nullableFieldIndex++);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            il.MarkLabel(setNullBit);
        }

        /// <summary>
        /// Emits instructions for serializing single value.
        ///
        /// Translates roughly to:
        ///     serializer.Serialize(actual.[op-value-name], buffer, offset);
        ///     offset += serializer.GetSizeFromValue(actual.[op-value-name]);
        /// </summary>
        public void EmitSerializeValue(SerializationValue value, int serializationValueIndex, int serializationValueCount)
        {
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            // Push local 'currentSerializer' to stack from local.
            EmitPushSerializationValueAddressToStack(il, value);

            // Push serialization value to stack.
            EmitPushSerializationValueToStack(il, value);
            
            // Box serialization value.
            il.Emit(OpCodes.Box, value.Type);
            // Push 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_2);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                                                                       
            // Call serialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);

            if (serializationValueIndex + 1 >= serializationValueCount) return;
            
            // Add last value serialized to the offset, push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_3);                    
            
            // Push 'currentSerializer' to stack.
            EmitPushSerializationValueAddressToStack(il, value);
            
            // Push last serialization value to stack.
            EmitPushSerializationValueToStack(il, value); 
                
            // Box serialization value.
            il.Emit(OpCodes.Box, value.Type);                                             
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
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
        public override void EmitLocals(int nullableValuesCount)
        {
            base.EmitLocals(nullableValuesCount);

            // Declare locals for null checks and masking if any of exist.
            if (nullableValuesCount == 0) return;
            
            var il = DynamicMethod.GetILGenerator();
            
            // Local 3: for masking null bit fields.
            Locals[localNullMask] = il.DeclareLocal(typeof(BitField));
            // Local 4: bit field serializer for serializing bit fields.
            Locals[localBitFieldSerializer] = il.DeclareLocal(typeof(IValueSerializer));
            // Local 5: null mask offset in the buffer.
            Locals[localNullMaskOffset] = il.DeclareLocal(typeof(int));
                
            // Determine how big of a bit field we need and instantiate bit field local.
            var moduloBitsInBitField = (nullableValuesCount % 8);

            // Add one additional byte to the field if we have any bits that don't fill all bytes.
            var bytesInBitField = (nullableValuesCount / 8) + (moduloBitsInBitField != 0 ? 1 : 0);
            
            // Instantiate local 'nullMask' bit field.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, bytesInBitField);
            il.Emit(OpCodes.Call, typeof(BitField).GetConstructor(new [] { typeof(int) })!);

            // Store current offset to local 'nullMaskOffset'.
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Stloc_S, Locals[localNullMaskOffset]);
            
            // Store bit field serializer to local.
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.BitFieldSerializer))!.GetMethod);
            il.Emit(OpCodes.Stloc_S, Locals[localBitFieldSerializer]);

            // Advance offset by the size of the bit field to leave space for it in front of the buffer.
            il.Emit(OpCodes.Ldarg_3);   
            // Push local 'bitFieldSerializer' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localBitFieldSerializer]);                                                          
            // Push local 'nullMask' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localNullMask]);
            // Box 'nullMask'.
            il.Emit(OpCodes.Box, typeof(BitField));
            // Call 'GetSizeFromValue', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 3);
        }
        
        /// <summary>
        /// Attempts to build the dynamic serialize delegate based on instructions received. Throws in case building the dynamic method fails.
        /// </summary>
        public DynamicSerializeDelegate Build(int nullableValuesCount)
        {
            var il = DynamicMethod.GetILGenerator();
            
            if (nullableValuesCount != 0)
            {
                // Serialize bit field, push local 'bitFieldSerializer' to stack.
                il.Emit(OpCodes.Ldloc_S, Locals[localBitFieldSerializer]);
                // Push local 'nullMask' to stack.
                il.Emit(OpCodes.Ldloc_S, Locals[localNullMask]);
                // Box serialization value.
                il.Emit(OpCodes.Box, typeof(BitField));
                // Push argument 'buffer' to stack.
                il.Emit(OpCodes.Ldarg_2);                                                                       
                // Push local 'nullMaskOffset' to stack.
                il.Emit(OpCodes.Ldloc_S, Locals[localNullMaskOffset]);                                                                       
                // Call serialize.
                il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Serialize))!);
            }
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicSerializeDelegate)DynamicMethod.CreateDelegate(typeof(DynamicSerializeDelegate));
                
                return (in ObjectSerializationContext context, object value, byte[] buffer, int offset) =>
                {
                    try
                    {
                        method(context, value, buffer, offset);
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