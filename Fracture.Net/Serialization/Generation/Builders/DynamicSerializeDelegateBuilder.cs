using System;
using System.Reflection.Emit;
using Fracture.Common.Reflection;
using Serilog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping serialization functions.
    /// </summary>
    public delegate void DynamicSerializeDelegate(object value, byte [] buffer, int offset);

    /// <summary>
    /// Class that provides dynamic functions building for dynamic serialization functions. 
    /// </summary>
    public sealed class DynamicSerializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Fields
        private int nullableValueIndex;

        private LocalBuilder localNullMask;
        private LocalBuilder localNullMaskOffset;
        #endregion

        public DynamicSerializeDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType)
            : base(valueRanges,
                   serializationType,
                   new DynamicMethodBuilder(
                       "Serialize",
                       typeof(void),
                       new []
                       {
                           typeof(object),  // Argument 0.
                           typeof(byte []), // Argument 1.
                           typeof(int)      // Argument 2.
                       }
                   ))
        {
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
            // Push serialization value to stack.
            EmitLoadSerializationValue(value);

            // Push null to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldnull);
            // Check if serialization value is null.
            DynamicMethodBuilder.Emit(OpCodes.Cgt_Un);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Brfalse_S, notNull);

            // Push value of the field to stack boxed.
            EmitLoadSerializationValue(value);

            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));

            if (serializationValueIndex + 1 < ValueRanges.SerializationValuesCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);

                // Push value from the nullable field to stack boxed.
                EmitLoadSerializationValue(value);

                DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
                // Advance offset by the size of the value.
                DynamicMethodBuilder.Emit(OpCodes.Add);
                DynamicMethodBuilder.Emit(OpCodes.Starg_S, 2);
            }

            // Branch out from the serialization of this value.
            var branchOut = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Br_S, branchOut);

            // Mask value to be null.
            DynamicMethodBuilder.MarkLabel(notNull);
            DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, localNullMask);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, nullableValueIndex++);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4_1);
            DynamicMethodBuilder.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            DynamicMethodBuilder.MarkLabel(branchOut);
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
            // Push serialization value to stack.
            EmitLoadSerializationValueAddressToStack(value);

            // Get boolean declaring if the nullable is null.
            DynamicMethodBuilder.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Brfalse_S, notNull);

            // Push value from the nullable field to stack boxed.
            EmitLoadSerializationValue(value);

            // Push argument 'buffer' to stack, push argument 'offset' to stack and then serialize the value.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));

            // Push value from the nullable field to stack boxed, increment serialization offset.
            if (serializationValueIndex + 1 < ValueRanges.SerializationValuesCount)
            {
                // Push argument 'offset', locals 'currentSerializer' and 'actual' to stack.
                DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);

                EmitLoadSerializationValue(value);

                DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
                // Advance offset by the size of the value.
                DynamicMethodBuilder.Emit(OpCodes.Add);
                DynamicMethodBuilder.Emit(OpCodes.Starg_S, 2);
            }

            // Branch out from the serialization of this value.
            var setNullBit = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Br_S, setNullBit);

            // Mask value to be null.
            DynamicMethodBuilder.MarkLabel(notNull);
            DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, localNullMask);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, nullableValueIndex++);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4_1);
            DynamicMethodBuilder.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.SetBit))!);
            DynamicMethodBuilder.MarkLabel(setNullBit);
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
            // Push serialization value to stack.
            EmitLoadSerializationValue(value);

            // Push 'buffer' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);
            // Call serialize.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSerializeMethodInfo(valueSerializerType, value.Type));

            if (serializationValueIndex + 1 >= ValueRanges.SerializationValuesCount) return;

            // Add last value serialized to the offset, push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);

            // Push last serialization value to stack.
            EmitLoadSerializationValue(value);

            // Call 'GetSizeFromValue', push size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            // Add offset + size.
            DynamicMethodBuilder.Emit(OpCodes.Add);
            // Store current offset to argument 'offset'.
            DynamicMethodBuilder.Emit(OpCodes.Starg_S, 2);
        }

        /// <summary>
        /// Emits instructions that declares initial locals for serialization.
        ///
        /// This roughly translates to:
        ///     var actual      = ([program-type])value;
        ///     var serializers = context.ValueSerializers;
        ///     var serializer  = default(ValueSerializer);
        /// </summary>
        public override void EmitLocals()
        {
            base.EmitLocals();

            EmitStoreArgumentValueToLocal();

            // Declare locals for null checks and masking if any of exist.
            if (ValueRanges.NullableValuesCount == 0) return;

            // Local 1: for masking null bit fields.
            localNullMask = DynamicMethodBuilder.DeclareLocal(typeof(BitField));
            // Local 2: null mask offset in the buffer.
            localNullMaskOffset = DynamicMethodBuilder.DeclareLocal(typeof(int));

            // Instantiate local 'nullMask' bit field.
            DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, localNullMask);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, BitField.LengthFromBits(ValueRanges.NullableValuesCount));
            DynamicMethodBuilder.Emit(OpCodes.Call, typeof(BitField).GetConstructor(new [] { typeof(int) })!);

            // Store current offset to local 'nullMaskOffset'.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localNullMaskOffset);

            // Advance offset by the size of the bit field to leave space for it in front of the buffer.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_2);
            // Push local 'nullMask' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localNullMask);
            // Call 'GetSizeFromValue', push size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(typeof(BitFieldSerializer)));
            // Add offset + size.
            DynamicMethodBuilder.Emit(OpCodes.Add);
            // Store current offset to argument 'offset'.
            DynamicMethodBuilder.Emit(OpCodes.Starg_S, 2);
        }

        /// <summary>
        /// Attempts to build the dynamic serialize delegate based on instructions received. Throws in case building the dynamic method fails.
        /// </summary>
        public DynamicSerializeDelegate Build()
        {
            if (ValueRanges.NullableValuesCount != 0)
            {
                // Push local 'nullMask' to stack.
                DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localNullMask);
                // Push argument 'buffer' to stack.
                DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
                // Push local 'nullMaskOffset' to stack.
                DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localNullMaskOffset);
                // Call serialize.
                DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSerializeMethodInfo(typeof(BitFieldSerializer)));
            }

            DynamicMethodBuilder.Emit(OpCodes.Ret);

            try
            {
                var method = (DynamicSerializeDelegate)DynamicMethodBuilder.CreateDelegate(typeof(DynamicSerializeDelegate));

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