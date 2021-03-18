using System;
using System.Runtime.Serialization;

namespace Fracture.Engine.Ecs
{
   [Serializable]
   public sealed class ComponentNotFoundException : Exception
   {
      public ComponentNotFoundException(int entityId)
         : base($"no component found for entity {entityId}")
      {
      }

      private ComponentNotFoundException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
   
   [Serializable]
   public sealed class ComponentDependencyException : Exception
   {
      #region Properties
      public Type DependingSystem
      {
         get;
      }
      
      public Type DependencySystem
      {
         get;
      }
      #endregion
      
      public ComponentDependencyException(int entityId, Type dependingSystem, Type dependencySystem, string message = "")
         : base($"{dependingSystem.Name} expecting entity to have component in system {dependencySystem.Name} present" + 
                (!string.IsNullOrEmpty(message) ? $": {message}" : ""))
      {
         DependingSystem  = dependingSystem;
         DependencySystem = dependencySystem;
      }

      private ComponentDependencyException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
}