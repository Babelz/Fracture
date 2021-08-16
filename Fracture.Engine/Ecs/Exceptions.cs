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
   }
   
   [Serializable]
   public sealed class ComponentDependencyException : Exception
   {
      public ComponentDependencyException(int entityId, Type dependingSystem, Type dependencySystem, string message = "")
         : base($"{dependingSystem.Name} expecting entity {entityId} to have component in system {dependencySystem.Name} " +
                $"present" + (!string.IsNullOrEmpty(message) ? $": {message}" : ""))
      {
         Data["DependingSystem"]  = dependingSystem;
         Data["DependencySystem"] = dependencySystem;
      }

      private ComponentDependencyException(SerializationInfo info, StreamingContext context) 
         : base(info, context)
      {
      }
   }
}