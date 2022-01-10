using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ecs
{
   public readonly struct TransformPositionEventArgs
   {
      #region Properties
      public int Id
      {
         get;
      }

      public Vector2 Position
      {
         get;
      }
      #endregion

      public TransformPositionEventArgs(int id, Vector2 position)
      {
         Id       = id;
         Position = position;
      }
   }
   
   public readonly struct TransformScaleEventArgs
   {
      #region Properties
      public int Id
      {
         get;
      }

      public Vector2 Scale
      {
         get;
      }
      #endregion

      public TransformScaleEventArgs(int id, Vector2 scale)
      {
         Id    = id;
         Scale = scale;
      }
   }
   
   public readonly struct TransformRotationEventArgs
   {
      #region Properties
      public int Id
      {
         get;
      }

      public float Rotation
      {
         get;
      }
      #endregion

      public TransformRotationEventArgs(int id, float rotation)
      {
         Id       = id;
         Rotation = rotation;
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
      #region Fields
      // Transform components data.
      private readonly LinearGrowthArray<Transform> transforms;
      
      // Transform component events.
      private readonly IUniqueEventPublisher<int, TransformPositionEventArgs> positionEvents;
      private readonly IUniqueEventPublisher<int, TransformScaleEventArgs> scaleEvents;
      private readonly IUniqueEventPublisher<int, TransformRotationEventArgs> rotationEvents;
      #endregion

      [BindingConstructor]
      public TransformComponentComponentSystem(IEntitySystem entities, IEventQueueSystem events)
         : base(entities, events)
      {
         positionEvents = events.CreateUnique<int, TransformPositionEventArgs>();
         scaleEvents    = events.CreateUnique<int, TransformScaleEventArgs>();
         rotationEvents = events.CreateUnique<int, TransformRotationEventArgs>();
         
         // Allocate data.
         transforms = new LinearGrowthArray<Transform>(128);
      }

      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         while (id >= transforms.Length) 
            transforms.Grow();

         return id;
      }

      public int Create(int entityId, in Transform transform)
      {
         // Initialize component and reserve enough storage.
         var id = InitializeComponent(entityId);
         
         // Store component data and state.
         transforms.Insert(id, transform);
         
         // Create events.
         positionEvents.Create(id);
         scaleEvents.Create(id);
         rotationEvents.Create(id);
         
         return id;
      }
      
      public int Create(int entityId)
         => Create(entityId, Transform.Default);

      public override bool Delete(int id)
      {
         if (!base.Delete(id))
            return false;
         
         // Delete events.
         positionEvents.Delete(id);
         scaleEvents.Delete(id);
         rotationEvents.Delete(id);
         
         // Reset state data.
         transforms.Insert(id, Transform.Default);

         return true;
      }

      public Vector2 GetPosition(int id)
      {
         AssertAlive(id);
         
         return transforms.AtIndex(id).Position;
      }

      public Vector2 GetScale(int id)
      {
         AssertAlive(id);
         
         return transforms.AtIndex(id).Scale;
      }

      public float GetRotation(int id)
      {
         AssertAlive(id);
         
         return transforms.AtIndex(id).Rotation;
      }

      public void TranslatePosition(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         // Update transform.
         transforms.Insert(id, Transform.TranslatePosition(transforms.AtIndex(id), translation));
         
         // Publish event about changes. Always refer to the field using the component
         // to get the latest value for the event.
         positionEvents.Publish(id, new TransformPositionEventArgs(id, transforms.AtIndex(id).Position));
      }

      public void TranslateScale(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TranslateScale(transforms.AtIndex(id), translation));
         
         scaleEvents.Publish(id, new TransformScaleEventArgs(id, transforms.AtIndex(id).Scale));
      }

      public void TranslateRotation(int id, float translation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TranslateRotation(transforms.AtIndex(id), translation));
         
         rotationEvents.Publish(id, new TransformRotationEventArgs(id, transforms.AtIndex(id).Rotation));
      }

      public void TransformPosition(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformPosition(transforms.AtIndex(id), transformation));
         
         positionEvents.Publish(id, new TransformPositionEventArgs(id, transforms.AtIndex(id).Position));
      }

      public void TransformScale(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformScale(transforms.AtIndex(id), transformation));
         
         scaleEvents.Publish(id, new TransformScaleEventArgs(id, transforms.AtIndex(id).Scale));
      }

      public void TransformRotation(int id, float transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformRotation(transforms.AtIndex(id), transformation));
         
         rotationEvents.Publish(id, new TransformRotationEventArgs(id, transforms.AtIndex(id).Rotation));
      }
   }
}