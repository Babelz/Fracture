using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

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
        private readonly byte localCurrentSerializer;
        private readonly byte localSerializers;
        private readonly byte localActual;
        
        private byte localIndexCounter;
        #endregion

        #region Properties
        /// <summary>
        /// Gets local variables that are accessed by indices.
        /// </summary>
        protected LocalBuilder[] Locals
        {
            get;
        }
        
        /// <summary>
        /// Get local temporary variables accessed by type.
        /// </summary>
        protected Dictionary<Type, LocalBuilder> Temp
        {
            get;
        }
        
        protected DynamicMethod DynamicMethod
        {
            get;
        }
        
        protected Type SerializationType
        {
            get;
        }
        #endregion
        
        public DynamicSerializationDelegateBuilder(Type serializationType, DynamicMethod dynamicMethod, int maxLocals)
        {
            SerializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
            DynamicMethod     = dynamicMethod ?? throw new ArgumentNullException(nameof(dynamicMethod));
            
            Locals = new LocalBuilder[MaxLocals + maxLocals];
            Temp   = new Dictionary<Type, LocalBuilder>();
            
            localCurrentSerializer = AllocateNextLocalIndex();
            localSerializers       = AllocateNextLocalIndex();
            localActual            = AllocateNextLocalIndex();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int ComputeNullMaskSize(int nullableValuesCount)
        {
            // Determine how big of a bit field we need and instantiate bit field local.
            var moduloBitsInBitField = (nullableValuesCount % 8);

            // Add one additional byte to the field if we have any bits that don't fill all bytes.
            return (nullableValuesCount / 8) + (moduloBitsInBitField != 0 ? 1 : 0);
        }

        protected byte AllocateNextLocalIndex()
            => checked(localIndexCounter++);
        
        protected void EmitPushCurrentSerializerToStack(ILGenerator il)
            => il.Emit(OpCodes.Ldloc_S, Locals[localCurrentSerializer]);
        
        /// <summary>
        /// Emits instructions for loading serialization value from object that is on top of the stack. The value can loaded from property and from field
        /// by pushing the actual value or the address.
        /// </summary>
        protected void EmitPushSerializationValueAddressToStack(ILGenerator il, SerializationValue value)
        {
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localActual]);
            
            if ((!value.IsValueType || value.IsNullable) && !Temp.ContainsKey(value.Type))
            {
                // Declare new local for the new nullable type.
                Temp.Add(value.Type, il.DeclareLocal(value.Type));
            }

            if (value.IsProperty)
            {
                var localNullable = Temp[value.Type];
                
                il.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
                
                il.Emit(OpCodes.Stloc, localNullable);
                il.Emit(OpCodes.Ldloca_S, localNullable);
            }
            else
                il.Emit(OpCodes.Ldflda, value.Field);
        }
        
        protected void EmitPushSerializationValueToStack(ILGenerator il, SerializationValue value)
        { 
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localActual]);

            if (value.IsProperty) 
                il.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
            else 
                il.Emit(OpCodes.Ldfld, value.Field);
        }
        
        /// <summary>
        /// Emits instructions for getting the next value serializer for serialization.
        ///
        /// Translates roughly to:
        ///     serializer = serializers[serializationValueIndex];
        /// </summary>
        protected void EmitStoreSerializerAtIndexToLocal(int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Load local 'serializers' to stack.
            il.Emit(OpCodes.Ldloc_S, Locals[localSerializers]);
            // Get current value serializer, push current serializer index to stack.
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex);
            // Push serializer at index to stack.
            il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<IValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
            // Store current serializer to local.
            il.Emit(OpCodes.Stloc, Locals[localCurrentSerializer]);
        }
        
        public virtual void EmitLocals(int nullableValuesCount)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            Locals[localActual] = il.DeclareLocal(SerializationType);                                     
            // Local 1: serializers.
            Locals[localSerializers] = il.DeclareLocal(typeof(IReadOnlyList<IValueSerializer>)); 
            // Local 2: local for storing the current serializer.
            Locals[localCurrentSerializer] = il.DeclareLocal(typeof(IValueSerializer));                               
            
            // Cast value to actual value, push argument 'value' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Cast or unbox value.
            il.Emit(SerializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox, SerializationType);
            // Store converted value to local 'actual'.
            il.Emit(OpCodes.Stloc_S, Locals[localActual]);                                                        
            
            // Get serializers to local, store serializers to local 'serializers'.
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.ValueSerializers))!.GetMethod);
            il.Emit(OpCodes.Stloc_S, Locals[localSerializers]);
        }
    }
}