using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Fracture.Engine.Physics;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ecs
{
    /// <summary>
    /// Interface for implementing physics body component systems. Bodies
    /// are used for physics interactions, detection collision and detecting
    /// entity surroundings. Body will automatically bind entity transform position
    /// and rotation.
    /// </summary>
    public interface IPhysicsBodyComponentSystem : IComponentSystem
    {
        /// <summary>
        /// Creates new body component and inserts to the physics simulation. 
        /// </summary>
        /// <param name="entityId">entity who will own this component</param>
        /// <param name="type">body type of the new body</param>
        /// <param name="position">initial position of body</param>
        /// <param name="rotation">initial rotation of the body</param>
        /// <param name="userData">optional user defined data for the body</param>
        /// <param name="shape">shape of the body</param>
        /// <param name="primary">boolean flag declaring if this body component is the primary body of entity,
        /// primary bodies rotation and position will be bound to transform components position and rotation</param>
        /// <returns></returns>
        int Create(int entityId,
                   BodyType type,
                   in Shape shape,
                   in Vector2 position = default,
                   float rotation = 0.0f,
                   object userData = null,
                   bool primary = false);

        Vector2 GetPosition(int componentId);
        float GetRotation(int componentId);
        Vector2 GetActiveLinearForce(int componentId);
        float GetActiveAngularForce(int componentId);
        Vector2 GetNextPosition(int componentId);
        float GetNextRotation(int componentId);
        Aabb GetBoundingBox(int componentId);
        BodyType GetType(int componentId);
        Shape GetShape(int componentId);
        IEnumerable<BodyComponentContact> GetCurrentContacts(int componentId);
        IEnumerable<BodyComponentContact> GetLeavingContacts(int componentId);
        object GetUserData(int componentId);

        bool IsPrimary(int componentId);

        void SetPosition(int componentId, Vector2 position);
        void SetRotation(int componentId, float rotation);

        void SetLinearVelocity(int componentId, Vector2 velocity);
        void SetAngularVelocity(int componentId, float velocity);

        void ApplyLinearImpulse(int componentId, float magnitude, Vector2 direction);
        void ApplyAngularImpulse(int componentId, float magnitude);

        void SetUserData(int componentId, object userData);

        /// <summary>
        /// Query surroundings of body component with given id.
        /// </summary>
        /// <param name="componentId">id of the component which surrounding will be queried</param>
        /// <param name="area">additional area added to components AABB for the query</param>
        /// <returns>body components that are inside the are of components AABB + area</returns>
        IEnumerable<int> QuerySurroundings(int componentId, float area);

        IEnumerable<int> RootQuery();

        IEnumerable<int> RayCastBroad(in Line line, Func<Body, bool> selector = null);
        IEnumerable<int> RayCastNarrow(in Line line);

        IEnumerable<int> AabbQueryBroad(in Aabb aabb, Func<Body, bool> selector = null);
        IEnumerable<int> AabbQueryNarrow(in Aabb aabb);
    }

    public readonly struct BodyComponentContact
    {
        #region Properties
        public int FirstComponentId
        {
            get;
        }

        public int SecondComponentId
        {
            get;
        }

        public Vector2 Translation
        {
            get;
        }
        #endregion

        public BodyComponentContact(int firstComponentId, int secondComponentId, Vector2 translation)
        {
            FirstComponentId  = firstComponentId;
            SecondComponentId = secondComponentId;
            Translation       = translation;
        }
    }

    public sealed class PhysicsBodyComponentSystem : SharedComponentSystem, IPhysicsBodyComponentSystem
    {
        #region Physics body component structure
        private struct PhysicsBodyComponent
        {
            #region Properties
            public int EntityId
            {
                get;
                set;
            }

            public Body Body
            {
                get;
                set;
            }

            public bool Primary
            {
                get;
                set;
            }

            public object UserData
            {
                get;
                set;
            }

            /// <summary>
            /// Gets set of current contacts.
            /// </summary>
            public List<BodyComponentContact> CurrentContacts
            {
                get;
                set;
            }

            /// <summary>
            /// Gets the last contacts.
            /// </summary>
            public List<BodyComponentContact> LastContacts
            {
                get;
                set;
            }

            /// <summary>
            /// Get set of leaving contacts.
            /// </summary>
            public List<BodyComponentContact> LeavingContacts
            {
                get;
                set;
            }

            /// <summary>
            /// Gets the entering contacts.
            /// </summary>
            public List<BodyComponentContact> EnteringContacts
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        #region Fields
        private readonly LinearGrowthList<PhysicsBodyComponent> components;

        private readonly ITransformComponentSystem transforms;
        private readonly IPhysicsWorldSystem       physics;

        private readonly List<int> dirtyComponentIds;
        #endregion

        [BindingConstructor]
        public PhysicsBodyComponentSystem(IEventQueueSystem events, 
                                          ITransformComponentSystem transforms,
                                          IPhysicsWorldSystem physics)
            : base(events)
        {
            this.transforms = transforms;
            this.physics    = physics;

            physics.BeginContact += Physics_BeginContact;
            physics.EndContact   += Physics_EndContact;
            physics.Moving       += Physics_Moving;
            
            components         =  new LinearGrowthList<PhysicsBodyComponent>();
            dirtyComponentIds  =  new List<int>();
        }

        #region Event handlers
        private void Physics_Moving(object sender, BodyEventArgs e)
        {
            dirtyComponentIds.Add((int)e.Body.UserData);
        }
        
        private void Physics_EndContact(object sender, BodyContactEventArgs e)
        {
            var firstComponentId  = (int)e.Body.UserData;
            var secondComponentId = (int)e.Contact.UserData;
        
            ref var firstComponent  = ref components.AtIndex(firstComponentId);
            ref var secondComponent = ref components.AtIndex(secondComponentId);
        
            firstComponent.LeavingContacts.Add(new BodyComponentContact(firstComponentId, secondComponentId, Vector2.Zero));
            secondComponent.LeavingContacts.Add(new BodyComponentContact(secondComponentId, firstComponentId, Vector2.Zero));
        
            dirtyComponentIds.Add(firstComponentId);
            dirtyComponentIds.Add(secondComponentId);
        }
        
        private void Physics_BeginContact(object sender, BodyContactEventArgs e)
        {
            var firstComponentId  = (int)e.Body.UserData;
            var secondComponentId = (int)e.Contact.UserData;
        
            ref var firstComponent  = ref components.AtIndex(firstComponentId);
            ref var secondComponent = ref components.AtIndex(secondComponentId);
        
            firstComponent.EnteringContacts.Add(new BodyComponentContact(firstComponentId, secondComponentId, Vector2.Zero));
            secondComponent.EnteringContacts.Add(new BodyComponentContact(secondComponentId, firstComponentId, Vector2.Zero));
        
            dirtyComponentIds.Add(firstComponentId);
            dirtyComponentIds.Add(secondComponentId);
        }
        #endregion

        private IEnumerable<int> GetQueryResults(QuadTreeNodeLink link)
        {
            // // Take first link bodies as result object and contact rest of the nodes to this.
            // var results = link.Bodies.Select(b => (int)world.Bodies.WithId(b).UserData);
            //
            // // Advance to next link.
            // link = link.Next;
            //
            // while (!link.End)
            // {
            //     // Contact next links bodies to results and advance.
            //     results = results.Concat(link.Bodies.Select(b => (int)world.Bodies.WithId(b).UserData));
            //
            //     link = link.Next;
            // }
            //
            // return results;
            return Array.Empty<int>();
        }

        private void UpdateBodyDirtyState(int componentId)
        {
            ref var component = ref components.AtIndex(componentId);

            // Do not mark dirty if body modified was not primary.
            if (!component.Primary)
                return;

            // Flag for next update to sync transform data from body to transform component.
            dirtyComponentIds.Add(componentId);
        }

        public int Create(int entityId,
                          BodyType type,
                          in Shape shape,
                          in Vector2 position = default,
                          float rotation = 0.0f,
                          object userData = null,
                          bool primary = false)
        {
            // Create component.
            var componentId = InitializeComponent(entityId);

            // Create actual body. Store component id as user data for the actual body
            // for reverse lookups.
            var body = physics.Create(type, shape, position, rotation);

            body.UserData = componentId;
            
            if (primary)
            {
                if (AllFor(entityId).Count(IsPrimary) != 0)
                    throw new InvalidOperationException($"{entityId} already has primary body");
            }

            // Store component data and state.
            ref var component = ref components.AtIndex(componentId);

            component.EntityId         =   entityId;
            component.Body             =   body;
            component.Primary          =   primary;
            component.UserData         =   userData;
            component.CurrentContacts  ??= new List<BodyComponentContact>();
            component.LastContacts     ??= new List<BodyComponentContact>();
            component.EnteringContacts ??= new List<BodyComponentContact>();
            component.LeavingContacts  ??= new List<BodyComponentContact>();

            return componentId;
        }

        public override bool Delete(int componentId)
        {
            ref var component = ref components.AtIndex(componentId);

            component.CurrentContacts.Clear();
            component.LastContacts.Clear();
            component.EnteringContacts.Clear();
            component.LeavingContacts.Clear();

            // Delete world data.           
            physics.Delete(component.Body);

            dirtyComponentIds.Remove(componentId);

            return base.Delete(componentId);
        }

        public Vector2 GetPosition(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.Position;
        }

        public float GetRotation(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.Angle;
        }

        public Vector2 GetActiveLinearForce(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.PositionTranslation;
        }

        public float GetActiveAngularForce(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.AngleTranslation;
        }

        public Vector2 GetNextPosition(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.PositionTransform;
        }

        public float GetNextRotation(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Body.AngleTransform;
        }

        public Aabb GetBoundingBox(int componentId)
        {
            AssertAlive(componentId);

            return default; // world.Bodies.WithId(components.AtIndex(componentId).Body).BoundingBox;
        }

        public BodyType GetType(int componentId)
        {
            AssertAlive(componentId);

            return default; // world.Bodies.WithId(components.AtIndex(componentId).Body).Type;
        }

        public Shape GetShape(int componentId)
        {
            AssertAlive(componentId);

            return default; // world.Bodies.WithId(components.AtIndex(componentId).Body).Shape;
        }

        public IEnumerable<BodyComponentContact> GetCurrentContacts(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).CurrentContacts;
        }

        public IEnumerable<BodyComponentContact> GetLeavingContacts(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).LeavingContacts;
        }

        public object GetUserData(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).UserData;
        }

        public bool IsPrimary(int componentId)
        {
            AssertAlive(componentId);

            return components.AtIndex(componentId).Primary;
        }

        public void SetPosition(int componentId, Vector2 position)
        {
            AssertAlive(componentId);

            // world.Bodies.WithId(components.AtIndex(componentId).Body).ForcedPosition = position;

            UpdateBodyDirtyState(componentId);
        }

        public void SetRotation(int componentId, float rotation)
        {
            AssertAlive(componentId);

            // world.Bodies.WithId(components.AtIndex(componentId).Body).ForcedRotation = rotation;

            UpdateBodyDirtyState(componentId);
        }

        public void SetLinearVelocity(int componentId, Vector2 velocity)
        {
            AssertAlive(componentId);

            components.AtIndex(componentId).Body.Translate(velocity);

            UpdateBodyDirtyState(componentId);
        }

        public void SetAngularVelocity(int componentId, float velocity)
        {
            AssertAlive(componentId);

            components.AtIndex(componentId).Body.Translate(velocity);
            
            UpdateBodyDirtyState(componentId);
        }

        public void ApplyLinearImpulse(int componentId, float magnitude, Vector2 direction)
            => SetLinearVelocity(componentId, magnitude * direction);

        public void ApplyAngularImpulse(int componentId, float magnitude)
            => SetAngularVelocity(componentId, magnitude);

        public void SetUserData(int componentId, object userData)
        {
            AssertAlive(componentId);

            components.AtIndex(componentId).UserData = userData;
        }

        public IEnumerable<int> QuerySurroundings(int componentId, float area)
        {
            AssertAlive(componentId);

            // ref var body = ref world.Bodies.WithId(components.AtIndex(componentId).Body);
            //
            // var link = world.AabbQueryBroad(new Aabb(body.Position,
            //                                          body.Rotation,
            //                                          body.BoundingBox.HalfBounds + new Vector2(area * 0.5f)));
            //
            // return GetQueryResults(link);
            return default;
        }

        public IEnumerable<int> RootQuery()
        {
            var link = physics.RootQuery();

            return GetQueryResults(link);
        }

        public IEnumerable<int> RayCastBroad(in Line line, Func<Body, bool> selector = null)
        {
            // var link = world.RayCastBroad(line, selector);
            //
            // return GetQueryResults(link);
            return default;
        }

        public IEnumerable<int> RayCastNarrow(in Line line)
        {
            // var link = world.RayCastNarrow(line);
            //
            // return GetQueryResults(link);
            return default;
        }

        public IEnumerable<int> AabbQueryBroad(in Aabb aabb, Func<Body, bool> selector = null)
        {
            var link = physics.AabbQueryBroad(aabb);

            return GetQueryResults(link);
        }

        public IEnumerable<int> AabbQueryNarrow(in Aabb aabb)
        {
            var link = physics.AabbQueryNarrow(aabb);

            return GetQueryResults(link);
        }

        public override void Update(IGameEngineTime time)
        {
            base.Update(time);

            if (dirtyComponentIds.Count == 0) return;
            
            foreach (var componentId in dirtyComponentIds)
            {
                ref var component = ref components.AtIndex(componentId);

                // Update contact states:
                // leaving, entering ->
                //    * clear last
                //    * remove leaving from current
                //    * add leaving to last
                //    * add entering to current
                //       * clear leaving
                //       * clear entering
                component.LastContacts.Clear();
            
                foreach (var leaving in component.LeavingContacts)
                {
                    component.CurrentContacts.Remove(leaving);
                    component.LastContacts.Add(leaving);
                }
            
                foreach (var entering in component.EnteringContacts)
                    component.CurrentContacts.Add(entering);
            
                component.LeavingContacts.Clear();
                component.EnteringContacts.Clear();
            
                // Update transform.    
                if (!transforms.BoundTo(component.EntityId))
                    continue;
            
                var transformId      = transforms.FirstFor(component.EntityId);
                var currentTransform = transforms.GetTransform(transforms.FirstFor(component.EntityId));
            
                transforms.ApplyTransformation(transformId, new Transform(component.Body.Position, currentTransform.Scale, component.Body.Angle));
            }
            
            dirtyComponentIds.Clear();
        }
    }
}