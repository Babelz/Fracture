using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.Core.Internal;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;

namespace Fracture.Net.Messages
{
    /// <summary>
    /// Exception throw when operation with message schema causes the application to enter the unsafe valley.
    /// </summary>
    public sealed class MessageSchemaTypeException : Exception
    {
        #region Properties
        public Type Type
        {
            get;
        }
        #endregion

        public MessageSchemaTypeException(Type type, string message, Exception inner = null)
            : base($"message schema operation for schema {type.Name} threw and exception: {message}", inner)
        {
            Type = type;
        }
    }
    
    /// <summary>
    /// Utility class for defining messaging schemas. Works only as wrapper around serialization library. For any more fine tuned schema mapping use the
    /// serialization type mappers.
    /// </summary>
    public static class MessageSchema
    {
        #region Schema attribute classes
        /// <summary>
        /// Attribute for annotating classes that contain message schemas.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class DescriptionAttribute : Attribute
        {
            public DescriptionAttribute()
            {
            }
        }

        /// <summary>
        /// Attribute for annotating methods for loading the message schema.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class LoadAttribute : Attribute
        {
            public LoadAttribute()
            {
            }
        }
        #endregion

        #region Fields
        private static readonly HashSet<Type> LoadedSchemas;
        #endregion

        static MessageSchema()
            => LoadedSchemas = new HashSet<Type>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateSchemaType(Type type)
        {
            if (!type.IsAbstract && !type.IsSealed)
                throw new MessageSchemaTypeException(type, "message schema types must be static");
            
            if (type.GetAttribute<DescriptionAttribute>() == null)
                throw new MessageSchemaTypeException(type, $"type is not annotated with {nameof(MessageSchema)}.{nameof(DescriptionAttribute)}");
            
            if (!type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Any(m => m.GetAttribute<LoadAttribute>() != null))
                throw new MessageSchemaTypeException(type, $"none of the types methods are annotated with {nameof(MessageSchema)}.{nameof(LoadAttribute)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LoadSchema(Type type)
        {
            LoadedSchemas.Add(type);
            
            try
            {
                foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public).Where(m => m.GetAttribute<LoadAttribute>() != null))
                {
                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        throw new MessageSchemaTypeException(type, "load schema method threw an exception", e);
                    }
                }
            }
            catch (Exception e)
            {
                throw new MessageSchemaTypeException(type, "loading schema failed", e);
            }
        }

        /// <summary>
        /// Defines message inside the schema and maps it for usage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForMessage<T>(ObjectSchemaMapDelegate map) where T : IMessage
        {
            var mapper = ObjectSerializationMapper.ForType<T>();

            map(mapper);

            var mapping = mapper.Map();

            StructSerializer.Map(mapping);
        }

        /// <summary>
        /// Defines structure that can be found inside the messages and maps it for usage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForStruct<T>(ObjectSchemaMapDelegate map)
        {
            var mapper = ObjectSerializationMapper.ForType<T>();

            map(mapper);

            var mapping = mapper.Map();

            StructSerializer.Map(mapping);
        }

        /// <summary>
        /// Attempts to load schema of given type. Throws exception if the schema loading fails
        /// or tif the type does not contain valid schema mapping.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Load(Type schemaType)
        {
            if (schemaType == null)
                throw new ArgumentNullException(nameof(schemaType));
            
            if (LoadedSchemas.Contains(schemaType))
                return;
            
            ValidateSchemaType(schemaType);
            
            LoadSchema(schemaType);
        }
    }
}