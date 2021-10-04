using System;
using System.Collections.Generic;
using Fracture.Common.Reflection;
using System.Reflection.Emit;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Abstract base class for implement dynamic serialization delegate builders. This class provides common schematics across all serialization builders
    /// such as common locals and common intermediate language emit operations. 
    /// </summary>
    public abstract class DynamicSerializationDelegateBuilder
    {
        #region Fields
        private readonly Dictionary<Type, LocalBuilder> nullableLocals;
        
        private LocalBuilder localValue;
        #endregion

        #region Properties
        protected DynamicMethodBuilder DynamicMethodBuilder
        {
            get;
        }

        protected ObjectSerializationValueRanges ValueRanges
        {
            get;
        }

        protected Type SerializationType
        {
            get;
        }
        #endregion

        protected DynamicSerializationDelegateBuilder(in ObjectSerializationValueRanges valueRanges, 
                                                      Type serializationType, 
                                                      DynamicMethodBuilder dynamicMethodBuilder)
        {
            ValueRanges          = valueRanges;
            SerializationType    = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
            DynamicMethodBuilder = dynamicMethodBuilder ?? throw new ArgumentNullException(nameof(dynamicMethodBuilder));
            
            nullableLocals = new Dictionary<Type, LocalBuilder>();
        }

        /// <summary>
        /// Emits instructions for loading serialization value from object that is on top of the stack. The value can loaded from property and from field
        /// by pushing the actual value or the address.
        /// </summary>
        protected void EmitLoadSerializationValueAddressToStack(SerializationValue value)
        {
            // Push local 'value' to stack.
            EmitLoadLocalValue();
            
            if ((!value.IsValueType || value.IsNullable) && !nullableLocals.ContainsKey(value.Type))
            {
                // Declare new local for the new nullable type.
                // temp.Add(value.Type, il.DeclareLocal(value.Type));
                // TODO: check this.
                nullableLocals.Add(value.Type, DynamicMethodBuilder.DeclareLocal(value.Type));
            }

            if (value.IsProperty)
            {
                // TODO: check if we can get rid of this nullable local stuff.
                var nullableLocal = nullableLocals[value.Type];
                
                DynamicMethodBuilder.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
                
                DynamicMethodBuilder.Emit(OpCodes.Stloc_S, nullableLocal);
                DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, nullableLocal);
            }
            else
                DynamicMethodBuilder.Emit(OpCodes.Ldflda, value.Field);
        }
        
        protected void EmitLoadSerializationValue(SerializationValue value)
        { 
            // Push local 'value' to stack.
            EmitLoadLocalValue();

            if (value.IsProperty) 
                DynamicMethodBuilder.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
            else 
                DynamicMethodBuilder.Emit(OpCodes.Ldfld, value.Field);
        }

        protected void EmitLoadLocalValue()
        {
            DynamicMethodBuilder.Emit(SerializationType.IsClass ? OpCodes.Ldloc_S : OpCodes.Ldloca_S, localValue);
        }
        
        protected void EmitBoxLocalValue()
        {
            DynamicMethodBuilder.Emit(OpCodes.Ldloc_S, localValue);
            DynamicMethodBuilder.Emit(OpCodes.Box, SerializationType);
        }
        
        protected void EmitStoreValueToLocal()
        {
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localValue);
        }
        
        protected void EmitStoreArgumentValueToLocal()
        {
            // Cast value to actual value, push argument 'value' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);
            // Cast or unbox value.
            DynamicMethodBuilder.Emit(SerializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, SerializationType);
            
            EmitStoreValueToLocal();  
        }
        
        public virtual void EmitLocals()
        {
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            localValue = DynamicMethodBuilder.DeclareLocal(SerializationType);
        }
    }
}