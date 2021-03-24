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
            public const int ContentSize = sizeof(ushort);
            
            /// <summary>
            /// Size of the type id of the message.
            /// </summary>
            public const int TypeIdSize = sizeof(ushort);
            #endregion
            
            public static class Field
            {
                #region Constant fields
                /// <summary>
                /// Size of single fields type id.
                /// </summary>
                public const int TypeIdSize = sizeof(byte);
        
                /// <summary>
                /// If the field is a collection, contains the size of the length of it.
                /// </summary>
                public const int CollectionLengthSize = sizeof(ushort);
            
                /// <summary>
                /// If the field is a collection, contains the size of the generic type id.
                /// </summary>
                public const int CollectionTypeIdSize = sizeof(ushort);
            
                /// <summary>
                /// If the field is a dynamic field, contains the size of the field.
                /// </summary>
                public const int DynamicTypeSize = sizeof(ushort);
                #endregion
            }
        }
    }
}