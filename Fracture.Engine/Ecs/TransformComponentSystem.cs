using Fracture.Common.Collections;
using Fracture.Common.Events;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ecs
{
   public delegate void TransformPositionEventHandler(int id, Vector2 position);
   public delegate void TransformScaleEventHandler(int id, Vector2 scale);
   public delegate void TransformRotationEventHandler(int id, float rotation); 
   
   /// <summary>
   /// Interface for implementing transform component systems.
   /// </summary>
   public interface ITransformComponentSystem : IComponentSystem
   {
      #region Properties
      /// <summary>
      /// Event invoked when transform position changes.
      /// </summary>
      IEvent<int, TransformPositionEventHandler> PositionChanged
      {
         get;
      }
      /// <summary>
      /// Event invoked when transform scale changes.
      /// </summary>
      IEvent<int, TransformScaleEventHandler> ScaleChanged
      {
         get;
      }
      /// <summary>
      /// Event invoked when transform rotation changes.
      /// </summary>
      IEvent<int, TransformRotationEventHandler> RotationChanged
      {
         get;
      }
      #endregion
      
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
      private IEventQueue<int, TransformPositionEventHandler> positionEvents;
      private IEventQueue<int, TransformScaleEventHandler> scaleEvents;
      private IEventQueue<int, TransformRotationEventHandler> rotationEvents;
      #endregion
      
      #region Properties
      public IEvent<int, TransformPositionEventHandler> PositionChanged
         => positionEvents;
      
      public IEvent<int, TransformScaleEventHandler> ScaleChanged
         => scaleEvents;
      
      public IEvent<int, TransformRotationEventHandler> RotationChanged
         => rotationEvents;
      #endregion
      
      public TransformComponentComponentSystem()
      {
         // Allocate data.
         transforms = new LinearGrowthArray<Transform>(1024);
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
         
         // Reset state data.
         transforms.Insert(id, Transform.Default);
         
         // Remove all events.
         positionEvents.Delete(id);
         scaleEvents.Delete(id);
         rotationEvents.Delete(id);
         
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
         positionEvents.Publish(id, e => e(id, transforms.AtIndex(id).Position));
      }

      public void TranslateScale(int id, in Vector2 translation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TranslateScale(transforms.AtIndex(id), translation));
         
         scaleEvents.Publish(id, e => e(id, transforms.AtIndex(id).Scale));
      }

      public void TranslateRotation(int id, float translation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TranslateRotation(transforms.AtIndex(id), translation));
         
         rotationEvents.Publish(id, e => e(id, transforms.AtIndex(id).Rotation));
      }

      public void TransformPosition(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformPosition(transforms.AtIndex(id), transformation));
         
         positionEvents.Publish(id, e => e(id, transforms.AtIndex(id).Position));
      }

      public void TransformScale(int id, in Vector2 transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformScale(transforms.AtIndex(id), transformation));
         
         scaleEvents.Publish(id, e => e(id, transforms.AtIndex(id).Scale));
      }

      public void TransformRotation(int id, float transformation)
      {
         AssertAlive(id);
         
         transforms.Insert(id, Transform.TransformRotation(transforms.AtIndex(id), transformation));
         
         rotationEvents.Publish(id, e => e(id, transforms.AtIndex(id).Rotation));
      }

      public override void Initialize(IGameEngine engine)
      {
         base.Initialize(engine);
         
         // Create events.
         var events = Engine.Systems.First<IEventQueueSystem>();
         
         positionEvents = events.CreateUnique<int, TransformPositionEventHandler>(EventQueueUsageHint.Normal);
         scaleEvents    = events.CreateUnique<int, TransformScaleEventHandler>(EventQueueUsageHint.Normal);
         rotationEvents = events.CreateUnique<int, TransformRotationEventHandler>(EventQueueUsageHint.Normal);
      }
   }
}