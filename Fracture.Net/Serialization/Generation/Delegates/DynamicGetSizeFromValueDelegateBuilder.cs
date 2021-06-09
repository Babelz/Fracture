using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Fracture.Net.Serialization.Generation.Delegates
{
    /// <summary>
    /// Delegate for wrapping functions for determining objects sizes from run types.
    /// </summary>
    public delegate ushort DynamicGetSizeFromValueDelegate(in ObjectSerializationContext context, object value);
    
    public sealed class DynamicGetSizeFromValueDelegateBuilder
    {
        #region Constant fields
        private const byte LocalCurrentSerializer = 0;
        private const byte LocalSerializers       = 1;
        private const byte LocalActual            = 2;
        private const byte LocalNullMask          = 3;
        
        private const int MaxLocals = 4;
        #endregion
        
        #region Fields
        // Local variables that are accessed by indices.
        private readonly LocalBuilder[] locals;
        
        // Local temporary variables accessed by type.
        private readonly Dictionary<Type, LocalBuilder> temp;
        
        private readonly DynamicMethod dynamicMethod;
        private readonly Type serializationType;
        #endregion
        
        public DynamicGetSizeFromValueDelegateBuilder(Type serializationType)
        {
            this.serializationType = serializationType ?? throw new ArgumentNullException(nameof(serializationType));
         
            dynamicMethod = new DynamicMethod(
                $"GetSizeFromValue", 
                typeof(void), 
                new []
                {
                    typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                    typeof(object)                                      // Argument 1.
                },
                true
            );

            locals = new LocalBuilder[MaxLocals];
            temp   = new Dictionary<Type, LocalBuilder>();
        }
        
        /// <summary>
        /// Emits instructions for loading serialization value from object that is on top of the stack. The value can loaded from property and from field
        /// by pushing the actual value or the address.
        /// </summary>
        private void EmitPushSerializationValueAddressToStack(ILGenerator il, SerializationValue value)
        {
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc_S, locals[LocalActual]);
            
            if (!temp.TryGetValue(value.Type, out var localNullable))
            {
                // Declare new local for the new nullable type.
                localNullable = il.DeclareLocal(value.Type);
                
                temp.Add(value.Type, localNullable);
            }
            
            if (value.IsProperty)
            {
                il.Emit(value.Property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.GetMethod);
                
                il.Emit(OpCodes.Stloc, localNullable);
                il.Emit(OpCodes.Ldloca_S, localNullable);
            }
            else
                il.Emit(OpCodes.Ldflda, value.Field);
        }
        
        private void EmitPushSerializationValueToStack(ILGenerator il, SerializationValue value)
        { 
            // Push local 'actual' to stack.
            il.Emit(OpCodes.Ldloc_S, locals[LocalActual]);

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
        private void EmitStoreSerializerAtIndexToLocal(int serializationValueIndex)
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Load local 'serializers' to stack.
            il.Emit(OpCodes.Ldloc_S, locals[LocalSerializers]);
            // Get current value serializer, push current serializer index to stack.
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex);
            // Push serializer at index to stack.
            il.Emit(OpCodes.Callvirt, typeof(IReadOnlyList<IValueSerializer>).GetProperties().First(p => p.GetIndexParameters().Length != 0).GetMethod);
            // Store current serializer to local.
            il.Emit(OpCodes.Stloc, locals[LocalCurrentSerializer]);
        }

        public void EmitLocals(int nullableFieldsCount)
        {
            var il = dynamicMethod.GetILGenerator();
            
            // Local 0: type we are serializing, create common locals for serialization. These are required across all serialization emit functions.
            locals[LocalActual] = il.DeclareLocal(serializationType);                                     
            // Local 1: serializers.
            locals[LocalSerializers] = il.DeclareLocal(typeof(IReadOnlyList<IValueSerializer>)); 
            // Local 2: local for storing the current serializer.
            locals[LocalCurrentSerializer] = il.DeclareLocal(typeof(IValueSerializer));                               
            
            // Cast value to actual value, push argument 'value' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Cast or unbox value.
            il.Emit(serializationType.IsClass ? OpCodes.Castclass : OpCodes.Unbox, serializationType);
            // Store converted value to local 'actual'.
            il.Emit(OpCodes.Stloc_S, locals[LocalActual]);                                                        
            
            // Get serializers to local, store serializers to local 'serializers'.
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.ValueSerializers))!.GetMethod);
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

        public DynamicGetSizeFromValueDelegate Build()
        {
            throw new NotImplementedException();
        }
    }
}