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
    public delegate object DynamicDeserializeDelegate(byte[] buffer, int offset);
    
    public sealed class DynamicDeserializeDelegateBuilder : DynamicSerializationDelegateBuilder
    {
        #region Constant fields
        private const int MaxLocals = 1;
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
        private readonly byte localNullMask;
        #endregion
        
        public DynamicDeserializeDelegateBuilder(in ObjectSerializationValueRanges valueRanges, Type serializationType) 
            : base(in valueRanges, 
                   serializationType,
                   new DynamicMethod(
                       $"Deserialize", 
                       typeof(object), 
                       new []
                       {
                           typeof(byte[]), // Argument 0.
                           typeof(int)     // Argument 1.
                       },
                       true
                   ),
                   MaxLocals)
        {
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
        
        public void EmitDeserializeNullableValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
         
            // Check if null mask contains null for this value at this index.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex - ValueRanges.NullableValuesOffset);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, isNull);
                
            EmitDeserializeValue(value, valueSerializerType, serializationValueIndex);
            il.MarkLabel(isNull);
        }

        public void EmitDeserializeValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Push local 'value' to stack.
            EmitLoadLocalValue(il);
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType, value.Type));
            
            // Load nullable value to stack if needed. This used to work before without this because of the boxing operations happening.
            if (value.IsNullable)
                il.Emit(OpCodes.Newobj, value.Type.GetConstructor(new [] { value.Type.GetGenericArguments()[0] })!);
           
            // Store deserialized value to target value.
            if (value.IsField)
                il.Emit(OpCodes.Stfld, value.Field);
            else
                il.Emit(value.Property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, value.Property.SetMethod);
            
            if (serializationValueIndex + 1 >= ValueRanges.SerializationValuesCount) return;
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);      
            // Call 'GetSizeFromBuffer', push size to stack.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType)); 
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 1);
        }

        public void EmitActivation(ConstructorInfo constructor)
        {
            var il = DynamicMethod.GetILGenerator();

            il.Emit(!constructor.DeclaringType!.IsValueType ? OpCodes.Newobj : OpCodes.Call, constructor);

            EmitStoreValueToLocal(il);
        }

        public void EmitLoadNullableValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
         
            // Check if null mask contains null for this value at this index.
            il.Emit(OpCodes.Ldloca_S, Locals[localNullMask]);
            il.Emit(OpCodes.Ldc_I4, serializationValueIndex - ValueRanges.NullableValuesOffset);
            il.Emit(OpCodes.Call, typeof(BitField).GetMethod(nameof(BitField.GetBit))!);
            
            var isNull = il.DefineLabel();
            il.Emit(OpCodes.Brtrue, isNull);
                
            EmitLoadValue(value, valueSerializerType, serializationValueIndex);
            il.MarkLabel(isNull);
            
            // Load default value in case the value is marked null in the null mask.
            EmitLoadDefaultValue(il, value);
        }

        public void EmitLoadValue(in SerializationValue value, Type valueSerializerType, int serializationValueIndex)
        {
            var il = DynamicMethod.GetILGenerator();
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(valueSerializerType, value.Type));
            
            // Push 'buffer' to stack. 
            il.Emit(OpCodes.Ldarg_0);                                                                       
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);      
            // Call 'GetSizeFromBuffer', push size to stack.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(valueSerializerType)); 
            // Push 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1); 
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 1);
        }

        public override void EmitLocals()
        {
            base.EmitLocals();
            
            if (ValueRanges.NullableValuesCount == 0) return;
            
            var il = DynamicMethod.GetILGenerator();

            Locals[localNullMask] = il.DeclareLocal(typeof(BitField));
            
            // Load argument 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_0);
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Call deserialize.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetDeserializeMethodInfo(typeof(BitFieldSerializer)));
            
            // Store deserialized bitfield to local 'nullMask'.
            il.Emit(OpCodes.Stloc_S, Locals[localNullMask]);
            
            // Load argument 'buffer' to stack.
            il.Emit(OpCodes.Ldarg_0);
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Call 'GetSizeFromBuffer', push size to stack.
            il.Emit(OpCodes.Call, ValueSerializerRegistry.GetSizeFromBufferMethodInfo(typeof(BitFieldSerializer)));
            // Load argument 'offset' to stack.
            il.Emit(OpCodes.Ldarg_1);
            // Add offset + size.
            il.Emit(OpCodes.Add);                                                              
            // Store current offset to argument 'offset'.
            il.Emit(OpCodes.Starg_S, 1);
        }

        public DynamicDeserializeDelegate Build()
        {
            var il = DynamicMethod.GetILGenerator();

            EmitLoadLocalValue(il);
            
            il.Emit(OpCodes.Ret);
            
            try
            {
                var method = (DynamicDeserializeDelegate)DynamicMethod.CreateDelegate(typeof(DynamicDeserializeDelegate));
                
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