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
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationValueRanges valueRanges, object value);
    
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
        private readonly byte localSize;
        
        private bool valuesSizeIsConst;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType)
            : base(valueRanges,
                   serializationType,
                   new DynamicMethod(
                       $"GetSizeFromValue", 
                       typeof(ushort), 
                       new []
                       {
                           typeof(ObjectSerializationValueRanges).MakeByRefType(), // Argument 0.
                           typeof(object)                                          // Argument 1.
                       },
                       true
                   ),
                   MaxLocals)
        {
            valuesSizeIsConst = true;
            
            localSize = AllocateNextLocalIndex();
        }
        
        private DynamicGetSizeFromValueDelegate CreateGetSizeFromValueDelegate(DynamicGetSizeFromValueDelegate method)
        {
            return (in ObjectSerializationValueRanges context, object value) =>
            {    
                try
                {
                    return method(context, value);
                }
                catch (Exception e)
                {
                    throw new DynamicGetSizeFromValueException(SerializationType, e, value);
                }
            };
        }

        private DynamicGetSizeFromValueDelegate CreateGetSizeFromValueAsConstClosure(DynamicGetSizeFromValueDelegate method)
        {
            ushort size = 0;
            
            return (in ObjectSerializationValueRanges context, object value) =>
            {
                if (size != 0u) return size;
                
                try
                {
                    size = method(context, value);
                }
                catch (Exception e)
                {
                    throw new DynamicGetSizeFromValueException(SerializationType, e, value);
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
            if (ValueRanges.NullableValuesCount == 0)
                return;
            
            var il = DynamicMethod.GetILGenerator();
            
            // Load bit field size to stack.
            il.Emit(OpCodes.Ldc_I4, BitField.LengthFromBits(ValueRanges.NullableValuesCount) + Protocol.NullMaskLength.Size);
            // Load local size to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localSize]);
            // Add local + bit field size.
            il.Emit(OpCodes.Add);
            // Store result to local size.
            il.Emit(OpCodes.Stloc_S, Locals[localSize]);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-value type value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name] != null)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfNonValueTypeValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            UpdateValuesSizeIsConstFlag(value);

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
            
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            il.Emit(OpCodes.Ldloc_S, Locals[localSize]);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_S, Locals[localSize]);
            il.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single nullable value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name].HasValue)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name].Value)
        /// </summary>
        public void EmitGetSizeOfNullableValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            UpdateValuesSizeIsConstFlag(value);

            var il = DynamicMethod.GetILGenerator();
            
            EmitLoadSerializationValueAddressToStack(il, value);
            
            // Get boolean declaring if the nullable is null.
            il.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, notNull);
            
            EmitLoadSerializationValueAddressToStack(il, value);
            
            il.Emit(OpCodes.Call, value.Type.GetProperty("Value")!.GetMethod);
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            il.Emit(OpCodes.Ldloc_S, Locals[localSize]);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_S, Locals[localSize]);
            il.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-nullable value type.
        ///
        /// Translates roughly to:
        ///     size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            EmitLoadSerializationValue(il, value);
            
            // Call get size from value.
            il.Emit(OpCodes.Call, ValueSerializerSchemaRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            
            // Push local size to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localSize]);
            // Add local + value size.
            il.Emit(OpCodes.Add);
            // Store results to local size.
            il.Emit(OpCodes.Stloc_S, Locals[localSize]);
        }
        
        public new void EmitLocals()
        {
            base.EmitLocals();
            
            var il = DynamicMethod.GetILGenerator();

            EmitStoreArgumentValueToLocal(il);
            
            // Local 0: total size of the value.
            Locals[localSize] = il.DeclareLocal(typeof(ushort));
        }

        public DynamicGetSizeFromValueDelegate Build()
        {            
            var il = DynamicMethod.GetILGenerator();
            
            il.Emit(OpCodes.Ldloc_S, Locals[localSize]);
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicGetSizeFromValueDelegate)DynamicMethod.CreateDelegate(typeof(DynamicGetSizeFromValueDelegate));
                
                if (ValueRanges.NullableValuesCount == 0 && valuesSizeIsConst)
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