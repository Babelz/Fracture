using System;
using System.Runtime.Serialization;

namespace Fracture.Common
{
   [Serializable]
   public sealed class MissingAttributeException : Exception
   {
      public MissingAttributeException(Type type, Type attribute)
         : base($"type {type.Name} is missing attribute {attribute.Name}")
      {
      }

      private MissingAttributeException(SerializationInfo info, StreamingContext context)
         : base(info, context)
      {
      }
   }
   
   [Serializable]
   public sealed class InvalidOrUnsupportedException : Exception
   {
      /// <summary>
      /// Creates new instance of <see cref="InvalidOrUnsupportedException"/>.
      /// </summary>
      /// <param name="what">the target that is invalid or unsupported</param>
      public InvalidOrUnsupportedException(string what)
         : base($"invalid or unsupported {what}")
      {
      }

      public InvalidOrUnsupportedException(string what, object value)
         : base($"invalid or unsupported {what} of value {value}")
      {
      }

      private InvalidOrUnsupportedException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
   
   /// <summary>
   /// Exception that represents a fatal runtime error that the program can't
   /// recover from. 
   /// </summary>
   [Serializable]
   public sealed class FatalRuntimeException : Exception
   {
      public FatalRuntimeException(string error)
         : base($"fatal runtime error occurred: {error}")
      {
      }
      
      private FatalRuntimeException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
   
   /// <summary>
   /// Exception that represents a fatal startup error that the program can't
   /// recover from. 
   /// </summary>
   [Serializable]
   public sealed class FatalStartupException : Exception
   {
      public FatalStartupException(string error)
         : base($"fatal startup error occurred: {error}")
      {
      }
      
      private FatalStartupException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
}