using System;
using System.Collections.Generic;

namespace Fracture.Net.Serialization
{
    [GenericValueSerializer]
    public static class ArraySerializer
    {
        #region Fields
        
        #endregion
        
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type.IsArray;
        
        /// <summary>
        /// Writes given array to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(T[] value, byte[] buffer, int offset) 
        {
            throw new NotImplementedException();
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as array
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static T[] Deserialize<T>(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of array, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of array value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(T[] value)
        {
            throw new NotImplementedException();
        }
    }
    
    [GenericValueSerializer]
    public static class ListSerializer
    {
        [ValueSerializer.SupportsType]
        public static bool SupportsType(Type type)
            => type == typeof(List<>);
        
        /// <summary>
        /// Writes given list to given buffer beginning at given offset.
        /// </summary>
        [ValueSerializer.Serialize]
        public static void Serialize<T>(List<T> value, byte[] buffer, int offset) 
        {
            throw new NotImplementedException();
        }
            
        /// <summary>
        /// Reads next n-bytes from given buffer beginning at given offset as list
        /// and returns that value to the caller.
        /// </summary>
        [ValueSerializer.Deserialize]
        public static List<T> Deserialize<T>(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of list, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromBuffer]
        public static ushort GetSizeFromBuffer(byte[] buffer, int offset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns size of list value, size will vary.
        /// </summary>
        [ValueSerializer.GetSizeFromValue]
        public static ushort GetSizeFromValue<T>(List<T> value)
        {
            throw new NotImplementedException();
        }
    }
}