using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Events;
using Fracture.Engine.Physics;
using Fracture.Engine.Physics.Dynamics;
using Fracture.Engine.Physics.Spatial;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ecs
{
   public readonly struct BodyContactEventArgs
   {
      #region Properties
      public int FirstBodyId
      {
         get;
      }

      public int SecondBodyId
      {
         get;
      }
      #endregion

      public BodyContactEventArgs(int firstBodyId, int secondBodyId)
      {
         FirstBodyId  = firstBodyId;
         SecondBodyId = secondBodyId;
      }
   }
   
   /// <summary>
   /// Interface for implementing physics body component systems. Bodies
   /// are used for physics interactions, detection collision and detecting
   /// entity surroundings. Body will automatically bind entity transform position
   /// and rotation.
   /// </summary>
   public interface IPhysicsBodyComponentSystem : IComponentSystem
   {
      #region Properties
      IEvent<int, BodyContactEventArgs> BeginContact
      {
         get;
      }
      
      IEvent<int, BodyContactEventArgs> EndContact
      {
         get;
      } 
      #endregion
      
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
      
      Vector2 GetPosition(int id);
      float GetRotation(int id);
      Vector2 GetActiveLinearForce(int id);
      float GetActiveAngularForce(int id);
      Vector2 GetNextPosition(int id);
      float GetNextRotation(int id);
      Aabb GetBoundingBox(int id);
      BodyType GetType(int id);
      Shape GetShape(int id);
      IEnumerable<int> GetContacts(int id);
      object GetUserData(int id);
      
      bool IsPrimary(int id);

      void SetPosition(int id, Vector2 position);
      void SetRotation(int id, float rotation);
      
      void SetLinearVelocity(int id, Vector2 velocity);
      void SetAngularVelocity(int id, float velocity);
      
      void ApplyLinearImpulse(int id, float magnitude, Vector2 direction);
      void ApplyAngularImpulse(int id, float magnitude);
      
      void SetUserData(int id, object userData);
      
      /// <summary>
      /// Query surroundings of body component with given id.
      /// </summary>
      /// <param name="id">id of the component which surrounding will be queried</param>
      /// <param name="area">additional area added to components AABB for the query</param>
      /// <returns>body components that are inside the are of components AABB + area</returns>
      IEnumerable<int> QuerySurroundings(int id, float area);
      
      IEnumerable<int> RootQuery();
        
      IEnumerable<int> RayCastBroad(in Line line, BodySelector selector = null);
      IEnumerable<int> RayCastNarrow(in Line line);
      
      IEnumerable<int> AabbQueryBroad(in Aabb aabb, BodySelector selector = null);
      IEnumerable<int> AabbQueryNarrow(in Aabb aabb);
   }

   public sealed class PhysicsBodyComponentSystem : SharedComponentSystem, IPhysicsBodyComponentSystem
   {
      #region Body dirty flags
      [Flags]
      private enum BodyDirtyFlags : byte
      {
         None     = 0,
         Position = (1 << 0),
         Rotation = (1 << 1),
      }
      #endregion
      
      #region Physics body component structure
      private struct PhysicsBodyComponent
      {
         #region Properties
         public int BodyId
         {
            get;
            set;
         }
         
         public int TransformId
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
         
         public BodyDirtyFlags DirtyFlags
         {
            get;
            set;
         }
         #endregion
      }
      #endregion

      #region Fields
      private readonly LinearGrowthArray<PhysicsBodyComponent> bodies;
      
      private readonly IEventQueue<int, BodyContactEventArgs> beginContactEvents;
      private readonly IEventQueue<int, BodyContactEventArgs> endContactEvents;
      
      private readonly ITransformComponentSystem transforms;
      private readonly IPhysicsWorldSystem world;
      
      private readonly List<int> dirty;
      #endregion
      
      #region Properties
      public IEvent<int, BodyContactEventArgs> BeginContact
         => beginContactEvents;

      public IEvent<int, BodyContactEventArgs> EndContact
         => endContactEvents;
      #endregion

      [BindingConstructor]
      public PhysicsBodyComponentSystem(IEntitySystem entities,
                                        IEventQueueSystem events,
                                        IPhysicsWorldSystem world, 
                                        ITransformComponentSystem transforms)
         : base(entities, events)
      {
         this.world      = world ?? throw new ArgumentNullException(nameof(world));
         this.transforms = transforms ?? throw new ArgumentNullException(nameof(transforms));
         
         world.BeginContact += WorldOnBeginContact;
         world.EndContact   += WorldOnEndContact;
         world.Moved        += WorldOnRelocated;
         
         // Create events.
         beginContactEvents = events.CreateUnique<int, BodyContactEventArgs>();
         endContactEvents   = events.CreateUnique<int, BodyContactEventArgs>();
         
         bodies = new LinearGrowthArray<PhysicsBodyComponent>();
         dirty  = new List<int>();
      }

      #region Event handlers
      private void WorldOnRelocated(object sender, BodyEventArgs e)
         => UpdateBodyDirtyState((int)world.Bodies.WithId(e.BodyId).UserData, BodyDirtyFlags.Position);
      
      private void WorldOnEndContact(object sender, Physics.BodyContactEventArgs e)
      {
         var firstBodyComponentId  = (int)world.Bodies.WithId(e.FirstBodyId).UserData;
         var secondBodyComponentId = (int)world.Bodies.WithId(e.SecondBodyId).UserData;
         
         endContactEvents.Publish(firstBodyComponentId, new BodyContactEventArgs(firstBodyComponentId, secondBodyComponentId));
      }

      private void WorldOnBeginContact(object sender, Physics.BodyContactEventArgs e)
      {
         var firstBodyComponentId  = (int)world.Bodies.WithId(e.FirstBodyId).UserData;
         var secondBodyComponentId = (int)world.Bodies.WithId(e.SecondBodyId).UserData;
         
         beginContactEvents.Publish(firstBodyComponentId, new BodyContactEventArgs(firstBodyComponentId, secondBodyComponentId));
      }
      #endregion

      private IEnumerable<int> GetQueryResults(QuadTreeNodeLink link)
      {
         // Take first link bodies as result object and contact rest of the nodes to this.
         var results = link.Bodies.Select(b => (int)world.Bodies.WithId(b).UserData);
         
         // Advance to next link.
         link = link.Next;
         
         while (!link.End)
         {
            // Contact next links bodies to results and advance.
            results = results.Concat(link.Bodies.Select(b => (int)world.Bodies.WithId(b).UserData));
            
            link = link.Next;
         }
         
         return results;
      }
      
      private void UpdateBodyDirtyState(int id, BodyDirtyFlags flags)
      {
         ref var component = ref bodies.AtIndex(id);
         
         // Do not mark dirty if body modified was not primary.
         if (!component.Primary)
            return;
         
         // Flag for next update to sync transform data from body to transform component.
         if (component.DirtyFlags == BodyDirtyFlags.None)
            dirty.Add(id);
         
         // Mark parts of the transform to be updated based on the current dirty state
         // and received flag.
         if ((flags & BodyDirtyFlags.Position) == BodyDirtyFlags.Position &&
             (component.DirtyFlags & BodyDirtyFlags.Position) != BodyDirtyFlags.Position)
         {
            component.DirtyFlags |= BodyDirtyFlags.Position;
         }
         else if ((flags & BodyDirtyFlags.Rotation) == BodyDirtyFlags.Rotation &&
                  (component.DirtyFlags & BodyDirtyFlags.Rotation) != BodyDirtyFlags.Rotation)
         {
            component.DirtyFlags |= BodyDirtyFlags.Rotation;
         }
      }
         
      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         while (id >= bodies.Length) 
            bodies.Grow();
         
         return id;
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
         var id = InitializeComponent(entityId);
         
         // Create actual body. Store component id as user data for the actual body
         // for reverse lookups.
         var bodyId = world.Create(type, position, rotation, shape, id);
         
         // Resolve transform id if primary body.
         var transformId = 0;
         
         if (primary)
         {
            if (IndicesOf(entityId).Count(IsPrimary) != 0)
               throw new InvalidOperationException($"{entityId} already has primary body");
            
            if (transforms.BoundTo(entityId) == 0)
               throw new ComponentDependencyException(entityId, GetType(), transforms.GetType());
            
            if (type == BodyType.Static)
               throw new InvalidOperationException("static bodies can't be primary");
            
            transformId = transforms.FirstFor(entityId);
         }
         
         // Store component data and state.
         bodies.Insert(id, new PhysicsBodyComponent()
         {
            BodyId      = bodyId,
            TransformId = transformId,
            Primary     = primary,
            UserData    = userData
         });
         
         // Create events.
         if (!beginContactEvents.Exists(id))
            beginContactEvents.Create(id);
            
         if (!endContactEvents.Exists(id))
            endContactEvents.Create(id);

         return id;
      }

      public override bool Delete(int id)
      {
         // Delete world data.           
         world.Delete(bodies.AtIndex(id).BodyId);
         
         dirty.Remove(id);
         
         return base.Delete(id);
      }

      public Vector2 GetPosition(int id)
      {
         AssertAlive(id);
         
         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).Position;
      }

      public float GetRotation(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).Rotation;
      }

      public Vector2 GetActiveLinearForce(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).LinearVelocity;
      }

      public float GetActiveAngularForce(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).AngularVelocity;
      }

      public Vector2 GetNextPosition(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).ForcedPosition;
      }

      public float GetNextRotation(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).ForcedRotation;
      }

      public Aabb GetBoundingBox(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).BoundingBox;
      }

      public BodyType GetType(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).Type;
      }

      public Shape GetShape(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(bodies.AtIndex(id).BodyId).Shape;
      }

      public IEnumerable<int> GetContacts(int id)
      {
         AssertAlive(id);

         return world.ContactsOf(bodies.AtIndex(id).BodyId).Select(i => (int)world.Bodies.WithId(i).UserData);
      }
      
      public object GetUserData(int id)
      {
         AssertAlive(id);
         
         return bodies.AtIndex(id).UserData;
      }

      public bool IsPrimary(int id)
      {
         AssertAlive(id);

         return bodies.AtIndex(id).Primary;
      }

      public void SetPosition(int id, Vector2 position)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).ForcedPosition = position;
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Position);
      }

      public void SetRotation(int id, float rotation)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).ForcedRotation = rotation;
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Rotation);
      }

      public void SetLinearVelocity(int id, Vector2 velocity)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).SetLinearVelocity(velocity);
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Position);
      }

      public void SetAngularVelocity(int id, float velocity)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).SetAngularVelocity(velocity);
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Rotation);
      }

      public void ApplyLinearImpulse(int id, float magnitude, Vector2 direction)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).ApplyLinearImpulse(magnitude, direction);
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Position);
      }

      public void ApplyAngularImpulse(int id, float magnitude)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(bodies.AtIndex(id).BodyId).ApplyAngularImpulse(magnitude);
         
         UpdateBodyDirtyState(id, BodyDirtyFlags.Rotation);
      }

      public void SetUserData(int id, object userData)
      {
         AssertAlive(id);
         
         bodies.AtIndex(id).UserData = userData;
      }

      public IEnumerable<int> QuerySurroundings(int id, float area)
      {
         AssertAlive(id);
         
         ref var body = ref world.Bodies.WithId(bodies.AtIndex(id).BodyId);
         
         var link = world.AabbQueryBroad(new Aabb(body.Position, 
                                                  body.Rotation, 
                                                  body.BoundingBox.HalfBounds + new Vector2(area * 0.5f)));
         
         return GetQueryResults(link);
      }

      public IEnumerable<int> RootQuery()
      {   
         var link = world.RootQuery();
         
         return GetQueryResults(link);
      }

      public IEnumerable<int> RayCastBroad(in Line line, BodySelector selector = null)
      {
         var link = world.RayCastBroad(line, selector);
         
         return GetQueryResults(link);
      }

      public IEnumerable<int> RayCastNarrow(in Line line)
      {   
         var link = world.RayCastNarrow(line);
         
         return GetQueryResults(link);
      }

      public IEnumerable<int> AabbQueryBroad(in Aabb aabb, BodySelector selector = null)
      {
         var link = world.AabbQueryBroad(aabb);
         
         return GetQueryResults(link);
      }

      public IEnumerable<int> AabbQueryNarrow(in Aabb aabb)
      {   
         var link = world.AabbQueryNarrow(aabb);
         
         return GetQueryResults(link);
      }

      public void Update()
      {
         if (dirty.Count == 0) return;
         
         // Update transform 
         foreach (var id in dirty)
         {
            ref var component = ref bodies.AtIndex(id);
            ref var body      = ref world.Bodies.WithId(bodies.AtIndex(id).BodyId);
            
            if ((component.DirtyFlags & BodyDirtyFlags.Position) == BodyDirtyFlags.Position)
               transforms.TransformPosition(component.TransformId, body.Position);
            
            if ((component.DirtyFlags & BodyDirtyFlags.Rotation) == BodyDirtyFlags.Rotation)
               transforms.TransformRotation(component.TransformId, body.Rotation);     
            
            component.DirtyFlags = BodyDirtyFlags.None;
         }
         
         dirty.Clear();
      }
   }
}