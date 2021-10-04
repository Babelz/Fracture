using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Fracture.Common.Reflection;
using NLog;

namespace Fracture.Net.Serialization.Generation.Builders
{
    /// <summary>
    /// Delegate for wrapping deserialization functions.  
    /// </summary>
    public delegate object DynamicDeserializeDelegate(byte[] buffer, int offset);
    
    public sealed class DynamicDeserializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private static readonly Dictionary<Type, Action<DynamicMethodBuilder>> EmitLoadDefaultValueMap = new Dictionary<Type, Action<DynamicMethodBuilder>>
        {
            { typeof(sbyte),   dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(byte),    dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(short),   dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(ushort),  dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(int),     dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(uint),    dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(long),    dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(ulong),   dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(float),   dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(decimal), dmb => dmb.Emit(OpCodes.Ldc_I4_0) },
            { typeof(string),  dmb => dmb.Emit(OpCodes.Ldstr, string.Empty) }
        };
        #endregion
        
        #region Fields
        private LocalBuilder localNullMask;
        
        private int nullableValueIndex;
        #endregion
        
        public DynamicDeserializeDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType) 
            : base(in valueRanges, 
                   serializationType,
                   new DynamicMethodBuilder(
                       $"Deserialize", 
                       typeof(object), 
                       new []
                       {
                           typeof(byte[]), // Argument 0.
                           typeof(int)     // Argument 1.
                       }
                   ))
        {
        }
        
        private static void EmitLoadDefaultValue(DynamicMethodBuilder dynamicMethodBuilder, in SerializationValue value)
        {
            if (EmitLoadDefaultValueMap.TryGetValue(value.Type, out var emitLoadDefaultValue))
            {
                emitLoadDefaultValue(dynamicMethodBuilder);
                
                return;
            }
            
            dynamicMethodBuilder.Emit(OpCodes.Ldnull);
        }
        
        public void EmitDeserializeNullableValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            // Check if null mask contains null for this value at this index.
            DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, localNullMask);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, nullableValueIndex++);
            DynamicMethodBuilder.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Brtrue, isNull);
                
            EmitDeserializeValue(value, valueSerializerType, serializationValueIndex);
            DynamicMethodBuilder.MarkLabel(isNull);
        }

        public void EmitDeserializeValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            // Push local 'value' to stack.
            EmitLoadLocalValue();
            
            // Push 'buffer' to stack. 
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType, value.Type));
            
            // Store deserialized value to target value.
            if (value.IsField)
                DynamicMethodBuilder.Emit(OpCodes.Stfld, value.Field);
            else
                DynamicMethodBuilder.Emit(value.Property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.SetMethod);
            
            if (serializationValueIndex + 1 >= ValueRanges.SerializationValuesCount) return;
            
            // Push 'buffer' to stack. 
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);      
            // Call 'GetSizeFromBuffer', push size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType, value.Type)); 
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1); 
            // Add offset + size.
            DynamicMethodBuilder.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            DynamicMethodBuilder.Emit(OpCodes.Starg_S, 1);
        }

        public void EmitActivation(ConstructorInfo constructor)
        {
            DynamicMethodBuilder.Emit(OpCodes.Newobj, constructor);
                
            EmitStoreValueToLocal();
        }

        public void EmitLoadNullableValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            // Check if null mask contains null for this value at this index.
            DynamicMethodBuilder.Emit(OpCodes.Ldloca_S, localNullMask);
            DynamicMethodBuilder.Emit(OpCodes.Ldc_I4, nullableValueIndex++);
            DynamicMethodBuilder.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = DynamicMethodBuilder.DefineLabel();
            DynamicMethodBuilder.Emit(OpCodes.Brtrue, isNull);
                
            EmitLoadValue(value, valueSerializerType, serializationValueIndex);
            DynamicMethodBuilder.MarkLabel(isNull);
            
            // Load default value in case the value is marked null in the null mask.
            EmitLoadDefaultValue(DynamicMethodBuilder, value);
        }

        public void EmitLoadValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            // Push 'buffer' to stack. 
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType, value.Type));
            
            // Push 'buffer' to stack. 
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);      
            // Call 'GetSizeFromBuffer', push size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType)); 
            // Push 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1); 
            // Add offset + size.
            DynamicMethodBuilder.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            DynamicMethodBuilder.Emit(OpCodes.Starg_S, 1);
        }

        public override void EmitLocals()
        {
            base.EmitLocals();
            
            if (ValueRanges.NullableValuesCount == 0) return;
            
            localNullMask = DynamicMethodBuilder.DeclareLocal(typeof(BitField));
            
            // Load argument 'buffer' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);
            // Load argument 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(BitFieldSerializer)));
            
            // Store deserialized bitfield to local 'nullMask'.
            DynamicMethodBuilder.Emit(OpCodes.Stloc_S, localNullMask);
            
            // Load argument 'buffer' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_0);
            // Load argument 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Call 'GetSizeFromBuffer', push size to stack.
            DynamicMethodBuilder.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(BitFieldSerializer)));
            // Load argument 'offset' to stack.
            DynamicMethodBuilder.Emit(OpCodes.Ldarg_1);
            // Add offset + size.
            DynamicMethodBuilder.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            DynamicMethodBuilder.Emit(OpCodes.Starg_S, 1);
        }
        
        public DynamicDeserializeDelegate Build()
        {
            EmitBoxLocalValue();
                
            DynamicMethodBuilder.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicDeserializeDelegate)DynamicMethodBuilder.CreateDelegate(typeof(DynamicDeserializeDelegate));
                
                return (buffer, offset) =>
                {
                    try
                    {
                        return method(buffer, offset);
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