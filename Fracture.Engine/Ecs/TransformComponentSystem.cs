using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ecs
{
   public readonly struct TransformChangedEventArgs
   {
      #region Properties
      public int EntityId
      {
         get;
      }
      
      public Transform Translation
      {
         get;
      }

      public Transform Transform
      {
         get;
      }  
      #endregion

      public TransformChangedEventArgs(int entityId, in Transform translation, in Transform transform)
      {
         EntityId    = entityId;
         Translation = translation;
         Transform   = transform;
      }
   }
   
   /// <summary>
   /// Interface for implementing transform component systems.
   /// </summary>
   public interface ITransformComponentSystem : IComponentSystem
   {
      /// <summary>
      /// Creates new transform component with initial transform value.
      /// </summary>
      int Create(int entityId, in Transform transform);
      
      /// <summary>
      /// Creates new transform component with initial position set to zero,
      /// scale to one and rotation to zero.
      /// </summary>
      int Create(int entityId);
      
      Vector2 GetPosition(int id);
      Vector2 GetScale(int id);
      float GetRotation(int id);
      Transform GetTransform(int id);
      
      void ApplyTransformation(int id, in Transform transform);
      void ApplyTranslation(int id, in Transform translation);
      
      void TranslatePosition(int id, in Vector2 translation);
      void TranslateScale(int id, in Vector2 translation);
      void TranslateRotation(int id, float translation);
      
      void TransformPosition(int id, in Vector2 transformation);
      void TransformScale(int id, in Vector2 transformation);
      void TransformRotation(int id, float transformation);
   }
   
   /// <summary>
   /// Default implementation of <see cref="ITransformComponentSystem"/>.
   /// </summary>
   public sealed class TransformComponentComponentSystem : UniqueComponentSystem, ITransformComponentSystem
   {
      #region Constant fields
      private const int ComponentsCapacity = 128;
      #endregion
      
      #region Transform component structure
      private struct TransformComponent
      {
         #region Properties
         public int EntityId
         {
            get;
            set;
         }
         
         public Transform Transform
         {
            get;
            set;
         }
         #endregion
      }
      #endregion
      
      #region Fields
      // Transform components data.
      private readonly LinearGrowthArray<TransformComponent> components;
      
      // Transform component events.
      private readonly IUniqueEvent<int, TransformChangedEventArgs> changedEvent;
      #endregion

      [BindingConstructor]
      public TransformComponentComponentSystem(IEventQueueSystem events)
         : base(events)
      {
         changedEvent = events.CreateUnique<int, TransformChangedEventArgs>();
         
         // Allocate data.
         components = new LinearGrowthArray<TransformComponent>(ComponentsCapacity);
      }
      
      private static TransformChangedEventArgs AggregateTransformChanges(in TransformChangedEventArgs current, in TransformChangedEventArgs next)
         => new TransformChangedEventArgs(current.EntityId, current.Translation + next.Translation, next.Transform);

      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         while (id >= components.Length) 
            components.Grow();

         return id;
      }

      public int Create(int entityId, in Transform transform)
      {
         // Initialize component and reserve enough storage.
         var id = InitializeComponent(entityId);
         
         // Store component data and state.
         components.Insert(id, new TransformComponent()
         {
            EntityId  = entityId,
            Transform = transform
         });
         
         // Create events.
         changedEvent.Create(id);
         
         // Publish initial change.
         changedEvent.Publish(id, new TransformChangedEventArgs(entityId, Transform.Default, transform));
         
         return id;
      }
      
      public int Create(int entityId)
         => Create(entityId, Transform.Default);

      public override bool Delete(int id)
      {
         if (!base.Delete(id))
            return false;
         
         // Delete events.
         changedEvent.Create(id);
         
         return true;
      }

      public Vector2 GetPosition(int id)
      {
         AssertAlive(id);
         
         return components.AtIndex(id).Transform.Position;
      }

      public Vector2 GetScale(int id)
      {
         AssertAlive(id);
         
         return components.AtIndex(id).Transform.Scale;
      }

      public float GetRotation(int id)
      {
         AssertAlive(id);
         
         return components.AtIndex(id).Transform.Rotation;
      }
      
      public Transform GetTransform(int id)
      {
         AssertAlive(id);
         
         return components.AtIndex(id).Transform;
      }
      
      public void ApplyTransformation(int id, in Transform transform)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = transform;
         
         changedEvent.Publish(id,
                              new TransformChangedEventArgs(component.EntityId, Transform.Default, transform),
                              AggregateTransformChanges);
      }

      public void ApplyTranslation(int id, in Transform translation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform += translation;
         
         changedEvent.Publish(id,
                              new TransformChangedEventArgs(component.EntityId, component.Transform, translation),
                              AggregateTransformChanges);
      }

      public void TranslatePosition(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TranslatePosition(component.Transform, translation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.ComponentPosition(translation), component.Transform),
                              AggregateTransformChanges);
      }

      public void TranslateScale(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TranslateScale(component.Transform, translation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.ComponentScale(translation), component.Transform),
                              AggregateTransformChanges);
      }

      public void TranslateRotation(int id, float translation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TranslateRotation(component.Transform, translation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.ComponentRotation(translation), component.Transform),
                              AggregateTransformChanges);
      }

      public void TransformPosition(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TransformPosition(component.Transform, transformation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.Default, component.Transform),
                              AggregateTransformChanges);
      }

      public void TransformScale(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TransformScale(component.Transform, transformation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.Default, component.Transform),
                              AggregateTransformChanges);
      }

      public void TransformRotation(int id, float transformation)
      {
         AssertAlive(id);
         
         ref var component = ref components.AtIndex(id);
         
         component.Transform = Transform.TransformRotation(component.Transform, transformation);
         
         changedEvent.Publish(id, 
                              new TransformChangedEventArgs(component.EntityId, Transform.Default, component.Transform),
                              AggregateTransformChanges);
      }
   }
}