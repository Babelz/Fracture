using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Fracture.Common.Reflection;
using Serilog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(object value);
    
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
        #endregion
        
        #region Fields
        private LocalBuilder localSize;
        
        private bool valuesSizeIsConst;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType)
            : base(valueRanges,
                   serializationType,
                   new DynamicMethodBuilder(
                       $"GetSizeFromValue", 
                       typeof(ushort), 
                       new []
                       {
                           typeof(object) // Argument 0.
                       }
                   ))
        {
            valuesSizeIsConst = true;
        }
        
        private DynamicGetSizeFromValueDelegate CreateGetSizeFromValueDelegate(DynamicGetSizeFromValueDelegate method)
        {
            return value =>
            {    
                try
                {
                    return method(value);
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
            
            return value =>
            {
                if (size != 0u) return size;
                
                try
                {
                    size = method(value);
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
        ///     size += BitFieldSerializer.SizeFromValue(nullMask);
        /// </summary>
        public void EmitSizeOfNullMask()
        {
            if (ValueRanges.NullableValuesCount == 0)
                return;
            
            // Load bit field size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, BitField.LengthFromBits(ValueRanges.NullableValuesCount) + Protocol.ContentLength.Size);
            // Load local size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localSize);
            // Add local + bit field size.
            DynamicMethodBuilder.Emit(OpCodes.Add);
            // Store result to local size.
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localSize);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-value type value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name] != null)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfNonValueTypeValue(in SerializationValue value, Type valueSerializerType)
        {
            UpdateValuesSizeIsConstFlag(value);

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
            
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localSize);
            DynamicMethodBuilder.Emit(OpCodes.Add);
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localSize);
            DynamicMethodBuilder.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single nullable value.
        ///
        /// Translates roughly to:
        ///     if (actual.[op-value-name].HasValue)
        ///         size += serializer.GetSizeFromValue(actual.[op-value-name].Value)
        /// </summary>
        public void EmitGetSizeOfNullableValue(in SerializationValue value, Type valueSerializerType)
        {
            UpdateValuesSizeIsConstFlag(value);

            EmitLoadSerializationValueAddressToStack(value);
            
            // Get boolean declaring if the nullable is null.
            DynamicMethodBuilder.Emit(OpCodes.Call, value.Type.GetProperty("HasValue")!.GetMethod);

            // Branch based on the value, if it is not null proceed to serialize, else branch to mask it as null.
            var notNull = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Brfalse_S, notNull);
            
            EmitLoadSerializationValue(value);
            
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localSize);
            DynamicMethodBuilder.Emit(OpCodes.Add);
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localSize);
            DynamicMethodBuilder.MarkLabel(notNull);
        }

        /// <summary>
        /// Emits instructions for getting value size of single non-nullable value type.
        ///
        /// Translates roughly to:
        ///     size += serializer.GetSizeFromValue(actual.[op-value-name])
        /// </summary>
        public void EmitGetSizeOfValue(in SerializationValue value, Type valueSerializerType)
        {
            EmitLoadSerializationValue(value);
            
            // Call get size from value.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromValueMethodInfo(valueSerializerType, value.Type));
            
            // Push local size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localSize);
            // Add local + value size.
            DynamicMethodBuilder.Emit(OpCodes.Add);
            // Store results to local size.
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localSize);
        }
        
        public new void EmitLocals()
        {
            base.EmitLocals();
            
            EmitStoreArgumentValueToLocal();
            
            // Local 0: total size of the value.
            localSize = DynamicMethodBuilder.DeclareLocal(typeof(ushort));
        }

        public DynamicGetSizeFromValueDelegate Build()
        {   
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localSize);
            DynamicMethodBuilder.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicGetSizeFromValueDelegate)DynamicMethodBuilder.CreateDelegate(typeof(DynamicGetSizeFromValueDelegate));
                
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