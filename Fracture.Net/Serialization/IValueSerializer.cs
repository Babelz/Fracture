using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NLog;

namespace Fracture.Net.Serialization
{
    /// <summary>
    /// Interface for for creating type free serializers for serializing single values. These values
    /// can be anything from single primitive to more complex classes and specific types like lists.
    ///
    /// TODO: in future this could be replaced with some static classes and attributes so we can skip boxing.
    /// </summary>
    public interface IValueSerializer
    {
        /// <summary>
        /// Returns boolean declaring if this serializer supports given type.
        /// </summary>
        bool SupportsType(Type type);

        /// <summary>
        /// Serializes given value to given buffer starting at given offset.
        /// </summary>
        void Serialize(object value, byte[] buffer, int offset);

        /// <summary>
        /// Deserializes value from given buffer starting at given offset.
        /// </summary>
        object Deserialize(byte[] buffer, int offset);

        /// <summary>
        /// Gets the size of the value from given buffer at given offset. This size is the values size
        /// inside the buffer when it is serialized.
        /// </summary>
        ushort GetSizeFromBuffer(byte[] buffer, int offset);
        
        /// <summary>
        /// Returns the size of the value when it is serialized.
        /// </summary>
        ushort GetSizeFromValue(object value);
    }

    /// <summary>
    /// Static registry class that holds all value serializers found from loaded assemblies. 
    /// </summary>
    public static class ValueSerializerRegistry
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private static readonly List<IValueSerializer> Serializers;
        #endregion
        
        static ValueSerializerRegistry()
        {
            Serializers = GetSerializerTypes().Select(t => (IValueSerializer)Activator.CreateInstance(t)).ToList();
        }
        
        /// <summary>
        /// Gets all types that are assignable from <see cref="IValueSerializer"/>, are classes and are not abstract.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<Type> GetSerializerTypes()
        {
            var types = new List<Type>();
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                try
                {
                    types.AddRange(assembly.GetTypes()
                                           .Where(t => !t.IsAbstract && t.IsClass && typeof(IValueSerializer).IsAssignableFrom(t))
                                           .OrderBy(t => t.Name));
                }   
                catch (ReflectionTypeLoadException e)
                {
                    Log.Warn(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            }
            
            return types;
        }    
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IValueSerializer GetValueSerializerForType(Type type)
            => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? 
                   Serializers.First(v => v.SupportsType(type.GenericTypeArguments[0])) : Serializers.First(v => v.SupportsType(type));
    }
}