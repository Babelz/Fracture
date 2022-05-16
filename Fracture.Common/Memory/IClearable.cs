using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Fracture.Common.Reflection;

namespace Fracture.Common.Memory
{
    /// <summary>
    /// Interface for implementing object state clearing operations.
    /// </summary>
    public interface IClearable
    {
        /// <summary>
        /// Clears the objects state to its initial state.
        /// </summary>
        void Clear();
    }

    public enum ClearTarget : byte
    {
        Field = 0,
        Property
    }

    public readonly struct ClearOption
    {
        #region Properties
        public string Name
        {
            get;
        }

        public ClearTarget Target
        {
            get;
        }
        #endregion

        private ClearOption(string name, ClearTarget target)
        {
            Name   = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Target = target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClearOption Property(string name) => new ClearOption(name, ClearTarget.Property);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClearOption Field(string name) => new ClearOption(name, ClearTarget.Field);
    }

    public delegate void ClearDelegate<T>(ref T value);

    public static class ClearableUtils
    {
        #region Constant fields
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Public;
        #endregion

        #region Static fields
        private static readonly Type [] NumericTypes =
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(decimal)
        };
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ClearDelegate<T> EmitClearDelegate<T>(IEnumerable<ClearOption> options)
        {
            static void EmitLoadDefaultValue(DynamicMethodBuilder methodBuilder, Type type)
            {
                if (NumericTypes.Contains(type) || type.IsEnum)
                    methodBuilder.Emit(OpCodes.Ldc_I4_0);
                else if (type == typeof(string))
                    methodBuilder.Emit(OpCodes.Ldstr, string.Empty);
                else
                    methodBuilder.Emit(OpCodes.Ldnull);
            }

            var methodBuilder = new DynamicMethodBuilder("Clear", typeof(void), new [] { typeof(T).MakeByRefType() });

            foreach (var option in options)
            {
                switch (option.Target)
                {
                    case ClearTarget.Field:
                        var field = typeof(T).GetField(option.Name, Flags);

                        if (field == null)
                            throw new InvalidOperationException($"could not locate field for specified clear option {option.Name} " +
                                                                $"in type {typeof(T).FullName}");

                        if (field.Name.Contains("_BackingField"))
                            continue;

                        methodBuilder.Emit(OpCodes.Ldarg_0);
                        methodBuilder.Emit(OpCodes.Ldind_Ref);

                        EmitLoadDefaultValue(methodBuilder, field.FieldType);

                        methodBuilder.Emit(OpCodes.Stfld, field);
                        break;
                    case ClearTarget.Property:
                        var property = typeof(T).GetProperty(option.Name, Flags);

                        if (property == null)
                            throw new InvalidOperationException($"could not locate property for specified clear option {option.Name} " +
                                                                $"in type {typeof(T).FullName}");

                        if (property.SetMethod.IsPrivate)
                            throw new InvalidOperationException($"can't clear value of property {property.Name} with private set method in type " +
                                                                $"{typeof(T).FullName}");

                        methodBuilder.Emit(OpCodes.Ldarg_0);
                        methodBuilder.Emit(OpCodes.Ldind_Ref);

                        EmitLoadDefaultValue(methodBuilder, property.PropertyType);

                        methodBuilder.Emit(property.SetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, property.SetMethod);
                        break;
                    default:
                        throw new InvalidOrUnsupportedException(nameof(ClearTarget), option);
                }
            }

            methodBuilder.Emit(OpCodes.Ret);

            return (ClearDelegate<T>)methodBuilder.CreateDelegate(typeof(ClearDelegate<T>));
        }

        private static List<FieldInfo> GetFields<T>() => typeof(T).GetFields(Flags).Where(f => !f.IsInitOnly).ToList();

        private static List<PropertyInfo> GetProperties<T>() => typeof(T).GetProperties(Flags).Where(p => p.CanWrite).ToList();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClearDelegate<T> CreateClearDelegate<T>()
        {
            var fields     = GetFields<T>();
            var properties = GetProperties<T>();

            fields.RemoveAll(f => properties.Any(p => p.PropertyType == f.FieldType && string.Equals(p.Name, f.Name, StringComparison.OrdinalIgnoreCase)));
            properties.RemoveAll(p => p.SetMethod.IsPrivate);

            return EmitClearDelegate<T>(fields.Select(
                                                   f => ClearOption.Field(f.Name))
                                              .Concat(properties.Select(p => ClearOption.Property(p.Name)))
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClearDelegate<T> CreateClearDelegate<T>(params ClearOption [] options)
        {
            var fields     = GetFields<T>();
            var properties = GetProperties<T>();

            if (options.Count(o => o.Target == ClearTarget.Field) != 0 &&
                !options.Where(o => o.Target == ClearTarget.Field).Any(o => fields.Any(f => f.Name == o.Name)))
                throw new InvalidOperationException($"field specified in options does not exists on type {typeof(T).FullName}");

            if (options.Count(o => o.Target == ClearTarget.Property) != 0 &&
                !options.Where(o => o.Target == ClearTarget.Property).Any(o => properties.Any(p => p.Name == o.Name)))
                throw new InvalidOperationException($"property specified in options does not exists on type {typeof(T).FullName}");

            return EmitClearDelegate<T>(options);
        }
    }
}