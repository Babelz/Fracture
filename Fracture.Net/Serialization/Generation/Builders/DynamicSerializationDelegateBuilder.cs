using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Abstract base class for implement dynamic serialization delegate builders. This class provides common schematics across all serialization generators
    /// such as common locals and common intermediate language emit operations. 
    /// </summary>
    public abstract class DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 3;
        #endregion
        
        #region Fields
        private readonly byte localValue;
        
        private byte localIndexCounter;
        
        // Temporary locals lookup.
        private readonly Dictionary<Type, LocalBuilder> temp;
        #endregion

        #region Properties
        /// <summary>
        /// Gets local variables that are accessed by indices.
        /// </summary>
        protected LocalBuilder[] Locals
        {
            get;
        }
        
        protected DynamicMethod DynamicMethod
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

        protected DynamicSerializationDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType, DynamicMethod dynamicMethod, int maxLocals)
        {
            ValueRanges       = valueRanges;
            SerializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
            DynamicMethod     = dynamicMethod ?? throw new ArgumentNullException(nameof(dynamicMethod));
            
            Locals = new LocalBuilder[MaxLocals + maxLocals];
            temp   = new Dictionary<Type, LocalBuilder>();
            
            localValue = AllocateNextLocalIndex();
        }

        protected byte AllocateNextLocalIndex()
            => checked(localIndexCounter++);
        
        /// <summary>
        /// Emits instructions for loading serialization value from object that is on top of the stack. The value can loaded from property and from field
        /// by pushing the actual value or the address.
        /// </summary>
        protected void EmitLoadSerializationValueAddressToStack(ILGenerator il, SerializationValue value)
        {
            // Push local 'value' to stack.
            EmitLoadLocalValue(il);
            
            if ((!value.IsValueType || value.IsNullable) && !temp.ContainsKey(value.Type))
            {
                // Declare new local for the new nullable type.
                temp.Add(value.Type, il.DeclareLocal(value.Type));
            }

            if (value.IsProperty)
            {
                var localNullable = temp[value.Type];
                
                il.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
                
                il.Emit(OpCodes.Stloc_S, localNullable);
                il.Emit(OpCodes.Ldloca_S, localNullable);
            }
            else
                il.Emit(OpCodes.Ldflda, value.Field);
        }
        
        protected void EmitLoadSerializationValue(ILGenerator il, SerializationValue value)
        { 
            // Push local 'value' to stack.
            EmitLoadLocalValue(il);

            if (value.IsProperty) 
                il.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
            else 
                il.Emit(OpCodes.Ldfld, value.Field);
        }

        protected void EmitLoadLocalValue(ILGenerator il)
        {
            il.Emit(!SerializationType.IsValueType ? OpCodes.Ldloc_S : OpCodes.Ldloca_S, Locals[localValue]);
        }
        
        protected void EmitStoreValueToLocal(ILGenerator il)
        {
            il.Emit(OpCodes.Stloc_S, Locals[localValue]);
        }
        
        protected void EmitStoreArgumentValueToLocal(ILGenerator il)
        {
            // Cast value to actual value, push argument 'value' to stack.
            il.Emit(OpCodes.Ldarg_0);
            // Cast or unbox value.
            il.Emit(SerializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox, SerializationType);
            
            EmitStoreValueToLocal(il);  
        }

        public virtual void EmitLocals()
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            Locals[localValue] = il.DeclareLocal(SerializationType);
        }
    }
}