using System;
using System.Reflection;
using System.Reflection.Emit;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object DynamicDeserializeDelegate(in ObjectSerializationContext context, byte[] buffer, int offset);
    
    public sealed class DynamicDeserializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 1;
        #endregion

        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        #region Fields
        private readonly byte localValue;
        
        private readonly byte localNullMask;
        #endregion
        
        public DynamicDeserializeDelegateBuilder(in ObjectSerializationContext context, Type serializationType) 
            : base(in context, 
                   serializationType,
                   new DynamicMethod(
                       $"Deserialize", 
                       typeof(object), 
                       new []
                       {
                           typeof(ObjectSerializationContext).MakeByRefType(), // Argument 0.
                           typeof(byte[]),                                     // Argument 1.
                           typeof(int)                                         // Argument 2.
                       },
                       true
                   ),
                   MaxLocals)
        {
            localValue    = AllocateNextLocalIndex();
            localNullMask = AllocateNextLocalIndex();
        }

        public void EmitDeserializeNonValueTypeValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitDeserializeNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitDeserializeValue(in SerializationValue value, int serializationValueIndex)
        { 
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();

            // Push actual to stack for storing the deserialized value.
            EmitLoadLocalValue(il);
            
            // Push local 'currentSerializer' to stack from local.
            EmitLoadCurrentSerializer(il);
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_1);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);      
            // Call deserialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Deserialize))!);
            
            // Unbox deserialized value.
            il.Emit(value.Type.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, value.Type);
            
            // Store deserialized value to target value.
            if (value.IsField)
                il.Emit(OpCodes.Stfld, value.Field);
            else
            {
                il.Emit(value.Property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.SetMethod);
            }

            if (serializationValueIndex + 1 >= Context.ValueSerializers.Count) return;
            
            // Push local 'currentSerializer' to stack from local.
            EmitLoadCurrentSerializer(il);
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_1);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);      
            // Call 'GetSizeFromBuffer', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromBuffer))!); 
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 2);
        }

        public void EmitActivation(ConstructorInfo constructor)
        {
            var il = DynamicMethod.GetILGenerator();

            il.Emit(!constructor.DeclaringType!.IsValueType ? OpCodes.Newobj : OpCodes.Call, constructor);

            EmitStoreValueToLocal(il);
        }

        public void EmitLoadNonValueTypeValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitLoadNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public void EmitLoadValue(in SerializationValue value, int serializationValueIndex)
        {
            throw new NotImplementedException();
        }

        public override void EmitLocals()
        {
            base.EmitLocals();
            
            if (Context.NullableValuesCount == 0) return;
            
            var il = DynamicMethod.GetILGenerator();

            Locals[localNullMask] = il.DeclareLocal(typeof(BitField));
            
            // Load argument 'context' to stack.
            il.Emit(OpCodes.Ldarg_0);
            // Load bit field serializer to stack.
            il.Emit(OpCodes.Call, typeof(ObjectSerializationContext).GetProperty(nameof(ObjectSerializationContext.BitFieldSerializer))!.GetMethod);
            il.Emit(OpCodes.Dup);
            // Load argument 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Dup);
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Dup);
            // Call deserialize.
            il.Emit(OpCodes.Call, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Deserialize))!);
            
            // Store deserialized bitfield to local 'nullMask'.
            il.Emit(OpCodes.Unbox, typeof(BitField));
            il.Emit(OpCodes.Stloc_S, Locals[localNullMask]);
            
            // Call 'GetSizeFromBuffer', push size to stack.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.GetSizeFromBuffer))!);
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 2);
        }

        public DynamicDeserializeDelegate Build()
        {
            var il = DynamicMethod.GetILGenerator();

            EmitLoadLocalValue(il);
            
            if (SerializationType.IsValueType)
                il.Emit(OpCodes.Box, SerializationType);
           
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicDeserializeDelegate)DynamicMethod.CreateDelegate(typeof(DynamicDeserializeDelegate));
                
                return (in ObjectSerializationContext context, byte[] buffer, int offset) =>
                {
                    try
                    {
                        return method(context, buffer, offset);
                    }
                    catch (Exception e)
                    {
                        throw new DynamicDeserializeException(SerializationType, e, buffer, offset);
                    }
                };
            } 
            catch (Exception e)
            {
                Log.Error(e, $"error occurred while building {nameof(DynamicDeserializeDelegate)}");
                
                throw;
            }
        }
    }
}