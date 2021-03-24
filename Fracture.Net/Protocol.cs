using System;
using System.Runtime.CompilerServices;
using Fracture.Common.Memory;

namespace Fracture.Net
{
    /// <summary>
    /// Static utility class containing protocol specific constants and functions.
    /// </summary>
    public static class Protocol
    {
        public static class Message
        {
            #region Constants
            /// <summary>
            /// Size of the whole message.
            /// </summary>
            public const ushort ContentSize = sizeof(ushort);
            
            /// <summary>
            /// Size of the type id of the message.
            /// </summary>
            public const ushort TypeIdSize = sizeof(ushort);
            #endregion
            
            public static class Field
            {
                #region Constant fields
                /// <summary>
                /// Size of single fields type id.
                /// </summary>
                public const ushort TypeIdSize = sizeof(byte);
                
                /// <summary>
                /// If the field is a collection, contains the size of generic type id.
                /// </summary>
                public const ushort GenericTypeIdSize = sizeof(ushort);
                
                /// <summary>
                /// IF the field is dynamic, contains the dynamic types length.
                /// </summary>
                public const ushort DynamicTypeLengthSize = sizeof(ushort);
                #endregion
            }
        }
    }
}