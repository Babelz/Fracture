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

        public Transform Delta
        {
            get;
        }
        #endregion

        public TransformChangedEventArgs(int entityId, in Transform transform, in Transform translation, in Transform delta)
        {
            EntityId    = entityId;
            Transform   = transform;
            Translation = translation;
            Delta       = delta;
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

        Vector2 GetPosition(int componentId);
        Vector2 GetScale(int componentId);
        float GetRotation(int componentId);
        Transform GetTransform(int componentId);

        void ApplyTransformation(int componentId, in Transform transform);
        void ApplyTranslation(int componentId, in Transform translation);

        void TranslatePosition(int componentId, in Vector2 translation);
        void TranslateScale(int componentId, in Vector2 translation);
        void TranslateRotation(int componentId, float translation);

        void TransformPosition(int componentId, in Vector2 transformation);
        void TransformScale(int componentId, in Vector2 transformation);
        void TransformRotation(int componentId, float transformation);
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
        private readonly LinearGrowthList<TransformComponent> components;

        // Transform component events.
        private readonly IUniqueEvent<int, TransformChangedEventArgs> changedEvent;
        #endregion

        [BindingConstructor]
        public TransformComponentComponentSystem(IEventQueueSystem events)
            : base(events)
        {
            changedEvent = events.CreateUnique<int, TransformChangedEventArgs>();

            // Allocate data.
            components = new LinearGrowthList<TransformComponent>(ComponentsCapacity);
        }

        private static TransformChangedEventArgs AggregateTransformChanges(in TransformChangedEventArgs current, in TransformChangedEventArgs next)
            => new TransformChangedEventArgs(current.EntityId, next.Transform, current.Translation + next.Translation, next.Delta + current.Delta);

        public int Create(int entityId, in Transform transform)
        {
            // Initialize component and reserve enough storage.
            var componentId = InitializeComponent(entityId);

            // Store component data and state.
            components.Insert(componentId,
                              new TransformComponent
                              {
                                  EntityId  = entityId,
                                  Transform = transform
                              });

            // Create events.
            changedEvent.Create(componentId);

            // Publish initial transform change.
            changedEvent.Publish(componentId, new TransformChangedEventArgs(entityId, transform, transform, transform));

            return componentId;
        }

        public int Create(int entityId)
            => Create(entityId, Transform.Default);

        public override bool Delete(int componentId)
        {
            if (!base.Delete(componentId))
                return false;

            // Delete events.
            changedEvent.Delete(componentId);

            return true;
        }

        public Vector2 GetPosition(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Transform.Position;
        }

        public Vector2 GetScale(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Transform.Scale;
        }

        public float GetRotation(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Transform.Rotation;
        }

        public Transform GetTransform(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Transform;
        }

        public void ApplyTransformation(int componentId, in Transform transform)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = transform;

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId, transform, Transform.Empty, component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void ApplyTranslation(int componentId, in Transform translation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform += translation;

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId, component.Transform, translation, component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TranslatePosition(int componentId, in Vector2 translation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TranslatePosition(component.Transform, translation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId,
                                                               component.Transform,
                                                               Transform.ComponentPosition(translation),
                                                               component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TranslateScale(int componentId, in Vector2 translation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TranslateScale(component.Transform, translation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId,
                                                               component.Transform,
                                                               Transform.ComponentScale(translation),
                                                               component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TranslateRotation(int componentId, float translation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TranslateRotation(component.Transform, translation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId,
                                                               component.Transform,
                                                               Transform.ComponentRotation(translation),
                                                               component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TransformPosition(int componentId, in Vector2 transformation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TransformPosition(component.Transform, transformation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId, component.Transform, Transform.Empty, component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TransformScale(int componentId, in Vector2 transformation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TransformScale(component.Transform, transformation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId, component.Transform, Transform.Empty, component.Transform - previous),
                                 AggregateTransformChanges);
        }

        public void TransformRotation(int componentId, float transformation)
        {
            AssertAlive(componentId);

            ref var component = ref components.AtIndex(componentId);

            var previous = component.Transform;

            component.Transform = Transform.TransformRotation(component.Transform, transformation);

            changedEvent.Publish(componentId,
                                 new TransformChangedEventArgs(component.EntityId, component.Transform, Transform.Empty, component.Transform - previous),
                                 AggregateTransformChanges);
        }
    }
}