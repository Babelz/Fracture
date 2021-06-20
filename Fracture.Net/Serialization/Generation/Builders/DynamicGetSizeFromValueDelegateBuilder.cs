using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationContext context, object value);
    
    public sealed class DynamicGetSizeFromValueDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Static fields
        private static readonly Predicate<SerializationValue>[] NonConstRunTypePredicates = 
        {
            (t) => t.Type.IsGenericType && t.Type.GetGenericTypeDefinition() == typeof(List<>),
            (t) => t.Type.IsGenericType && t.Type.GetGenericTypeDefinition() == typeof(Dictionary<,>),
            (t) => t.Type.IsArray,
            (t) => t.Type == typeof(string),
            (t) => t.IsNullAssignable
        };
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Constant fields
        private const int MaxLocals = 1;
        #endregion
        
        #region Fields
        private readonly byte LocalSize;
        
        private bool valuesSizeIsConst;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(in ObjectSerializationContext context, Type serializationType)
            : base(context,
                   serializationType,
                   new DynamicMethod(
                       $"GetSizeFromValue", 
                       typeof(ushort), 
                       new []
                       {
                           typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                           typeof(object)                                      // Argument 1.
                       },
                       true
                   ),
                   MaxLocals)
        {
            valuesSizeIsConst = true;
            
            LocalSize = AllocateNextLocalIndex();
        }
        
        private DynamicGetSizeFromValueDelegate CreateGetSizeFromValueDelegate(DynamicGetSizeFromValueDelegate method)
        {
            return (in ObjectSerializationContext context, object value) =>
            {    
                try
                {
                    return method(context, value);
                }
                catch (Exception e)
                {
                    throw new DynamicSerializeException(SerializationType, e, value);
                }
            };
        }

        private DynamicGetSizeFromValueDelegate CreateGetSizeFromValueAsConstClosure(DynamicGetSizeFromValueDelegate method)
        {
            ushort size = 0;
            
            return (in ObjectSerializationContext context, object value) =>
            {
                if (size != 0u) return size;
                
                try
                {
                    size = method(context, value);
                }
                catch (Exception e)
                {
                    throw new DynamicSerializeException(SerializationType, e, value);
                }

                return size;
            };
        }
        
        private void UpdateValuesSizeIsConstFlag(SerializationValue value)
        {
            if (!valuesSizeIsConst) return;
            
            if (NonConstRunTypePredicates.Any(p => p(value)))
                valuesSizeIsConst = false;
        }

        /// <summary>
        /// Emits instructions for getting value size of the possible null mask.
        ///
        /// Translate roughly to:
        ///     size += BitField.LengthFromBits(context.NullableValuesCount) + Protocol.NullMaskLength.Size;
        /// </summary>
        public void EmitSizeOfNullMask()
        {
            if (Context.NullableValuesCount == 0)
                return;
            
            var il = DynamicMethod.GetILGenerator();
            
            // Load bit field size to stack.
            il.Emit(OpCodes.Ldc_I4, BitField.LengthFromBits(Context.NullableValuesCount) + Protocol.NullMaskLength.Size);
            // Load local size to stack.
            il.Emit(OpCodes.Ldloc, Locals[LocalSize]);
            // Add local + bit field size.
            il.Emit(OpCodes.Add);
            // Store result to local size.
            il.Emit(OpCodes.Stloc, Locals[LocalSize]);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-value type value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name] != null)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfNonValueTypeValue(in SerializationValue value, int serializationValueIndex)
        {
            UpdateValuesSizeIsConstFlag(value);

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
            
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
            
            il.Emit(OpCodes.Ldloc, Locals[LocalSize]);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, Locals[LocalSize]);
            il.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single nullable value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name].HasValue)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name].Value)
        /// </summary>
        public void EmitGetSizeOfNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            UpdateValuesSizeIsConstFlag(value);

            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            EmitPushSerializationValueAddressToStack(il, value);
            
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            EmitPushCurrentSerializerToStack(il);
            
            EmitPushSerializationValueAddressToStack(il, value);
            
            il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
            il.Emit(OpCodes.Box, value.Type.GenericTypeArguments[0]);
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
            
            il.Emit(OpCodes.Ldloc, Locals[LocalSize]);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, Locals[LocalSize]);
            il.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-nullable value type.
        ///
        /// Translates roughly to:
        ///     size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfValue(in SerializationValue value, int serializationValueIndex)
        {
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            EmitPushCurrentSerializerToStack(il);
            
            EmitPushSerializationValueToStack(il, value);
            
            // Box serialization value.
            il.Emit(OpCodes.Box, value.Type);
            // Call get size from value.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromValue))!);
            // Push local size to stack.
            il.Emit(OpCodes.Ldloc, Locals[LocalSize]);
            // Add local + value size.
            il.Emit(OpCodes.Add);
            // Store results to local size.
            il.Emit(OpCodes.Stloc, Locals[LocalSize]);
        }
        
        public new void EmitLocals()
        {
            base.EmitLocals();
            
            var il = DynamicMethod.GetILGenerator();

            // Local 0: total size of the value.
            Locals[LocalSize] = il.DeclareLocal(typeof(ushort));
        }

        public DynamicGetSizeFromValueDelegate Build()
        {            
            var il = DynamicMethod.GetILGenerator();
            
            il.Emit(OpCodes.Ldloc, Locals[LocalSize]);
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicGetSizeFromValueDelegate)DynamicMethod.CreateDelegate(typeof(DynamicGetSizeFromValueDelegate));
                
                if (Context.NullableValuesCount == 0 && valuesSizeIsConst)
                    return CreateGetSizeFromValueAsConstClosure(method);
                
                return CreateGetSizeFromValueDelegate(method);
            } 
            catch (Exception e)
            {
                Log.Error(e, $"error occurred while building {nameof(DynamicSerializeDelegate)}");
                
                throw;
            }
        }
    }
}