using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Diagnostics.Tracing.Parsers.Tpl;
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
        private const int MaxLocals = 2;
        #endregion

        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private static readonly Dictionary<Type, Action<ILGenerator>> EmitLoadDefaultValueMap = new Dictionary<Type, Action<ILGenerator>>
        {
            { typeof(sbyte),   il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(byte),    il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(short),   il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(ushort),  il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(int),     il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(uint),    il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(long),    il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(ulong),   il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(float),   il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(decimal), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(string),  il => il.Emit(OpCodes.Ldstr, string.Empty) }
        };
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
        
        private static void EmitLoadDefaultValue(ILGenerator il, in SerializationValue value)
        {
            if (EmitLoadDefaultValueMap.TryGetValue(value.Type, out var emitLoadDefaultValue))
            {
                emitLoadDefaultValue(il);
                
                return;
            }
            
            il.Emit(OpCodes.Ldnull);
        }
        
        public void EmitDeserializeNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
         
            // Check if null mask contains null for this value at this index.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex - Context.NullableValuesOffset);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, isNull);
                
            EmitDeserializeValue(value, serializationValueIndex);
            il.MarkLabel(isNull);
        }

        public void EmitDeserializeValue(in SerializationValue value, int serializationValueIndex)
        { 
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            // Push local 'value' to stack.
            EmitLoadLocalValue(il);
            
            // Push local 'currentSerializer' to stack from local.
            EmitLoadCurrentSerializer(il);
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_1);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);      
            // Call deserialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Deserialize))!);
            
            il.Emit(OpCodes.Unbox_Any, value.Type);
            
            // Store deserialized value to target value.
            if (value.IsField)
                il.Emit(OpCodes.Stfld, value.Field);
            else
                il.Emit(value.Property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.SetMethod);
            
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

        public void EmitLoadNullableValue(in SerializationValue value, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
         
            // Check if null mask contains null for this value at this index.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex - Context.NullableValuesOffset);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, isNull);
                
            EmitLoadValue(value, serializationValueIndex);
            il.MarkLabel(isNull);
            
            // Load default value in case the value is marked null in the null mask.
            EmitLoadDefaultValue(il, value);
        }

        public void EmitLoadValue(in SerializationValue value, int serializationValueIndex)
        { 
            EmitStoreSerializerAtIndexToLocal(serializationValueIndex);
            
            var il = DynamicMethod.GetILGenerator();
            
            // Push local 'currentSerializer' to stack from local.
            EmitLoadCurrentSerializer(il);
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_1);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);      
            // Call deserialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Deserialize))!);
            
            il.Emit(OpCodes.Unbox_Any, value.Type);
            
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
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);
            // Call deserialize.
            il.Emit(OpCodes.Callvirt, typeof(IValueSerializer).GetMethod(nameof(IValueSerializer.Deserialize))!);
            
            // Store deserialized bitfield to local 'nullMask'.
            il.Emit(OpCodes.Unbox_Any, typeof(BitField));
            il.Emit(OpCodes.Stloc_S, Locals[localNullMask]);
            
            // Load argument 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_2);
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