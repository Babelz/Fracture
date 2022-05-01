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
using Fracture.Engine.Physics.Spatial;
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
      
      Vector2 GetPosition(int id);
      float GetRotation(int id);
      Vector2 GetActiveLinearForce(int id);
      float GetActiveAngularForce(int id);
      Vector2 GetNextPosition(int id);
      float GetNextRotation(int id);
      Aabb GetBoundingBox(int id);
      BodyType GetType(int id);
      Shape GetShape(int id);
      IEnumerable<BodyComponentContact> GetCurrentContacts(int id);
      IEnumerable<BodyComponentContact> GetLeavingContacts(int id);
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
         
         public int BodyId
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
      private readonly LinearGrowthArray<PhysicsBodyComponent> components;

      private readonly ITransformComponentSystem transforms;
      private readonly IPhysicsWorldSystem world;
      
      private readonly List<int> dirty;
      #endregion

      [BindingConstructor]
      public PhysicsBodyComponentSystem(IEventQueueSystem events,
                                        IPhysicsWorldSystem world, 
                                        ITransformComponentSystem transforms)
         : base(events)
      {
         this.world      = world;
         this.transforms = transforms;
         
         world.BeginContact += World_BeginContact;
         world.EndContact   += World_EndContact;
         world.Moved        += World_Relocated;

         components = new LinearGrowthArray<PhysicsBodyComponent>();
         dirty      = new List<int>();
      }

      #region Event handlers
      private void World_Relocated(object sender, BodyEventArgs e)
         => UpdateBodyDirtyState((int)world.Bodies.WithId(e.BodyId).UserData);
      
      private void World_EndContact(object sender, BodyContactEventArgs e)
      {
         var firstComponentId  = (int)world.Bodies.WithId(e.FirstBodyId).UserData;
         var secondComponentId = (int)world.Bodies.WithId(e.SecondBodyId).UserData;
         
         ref var firstComponent  = ref components.AtIndex(firstComponentId);
         ref var secondComponent = ref components.AtIndex(secondComponentId);
         
         firstComponent.LeavingContacts.Add(new BodyComponentContact(firstComponentId, secondComponentId, e.Translation));
         secondComponent.LeavingContacts.Add(new BodyComponentContact(secondComponentId, firstComponentId, e.Translation));
         
         dirty.Add(firstComponentId);
         dirty.Add(secondComponentId);
      }

      private void World_BeginContact(object sender, BodyContactEventArgs e)
      {
         var firstComponentId  = (int)world.Bodies.WithId(e.FirstBodyId).UserData;
         var secondComponentId = (int)world.Bodies.WithId(e.SecondBodyId).UserData;
         
         ref var firstComponent  = ref components.AtIndex(firstComponentId);
         ref var secondComponent = ref components.AtIndex(secondComponentId);
         
         firstComponent.EnteringContacts.Add(new BodyComponentContact(firstComponentId, secondComponentId, e.Translation));
         secondComponent.EnteringContacts.Add(new BodyComponentContact(secondComponentId, firstComponentId, e.Translation));

         dirty.Add(firstComponentId);
         dirty.Add(secondComponentId);
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
      
      private void UpdateBodyDirtyState(int id)
      {
         ref var component = ref components.AtIndex(id);
         
         // Do not mark dirty if body modified was not primary.
         if (!component.Primary)
            return;
         
         // Flag for next update to sync transform data from body to transform component.
         dirty.Add(id);
      }
         
      protected override int InitializeComponent(int entityId)
      {
         var id = base.InitializeComponent(entityId);
         
         while (id >= components.Length) 
            components.Grow();
         
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
         
         if (primary)
         {
            if (AllFor(entityId).Count(IsPrimary) != 0)
               throw new InvalidOperationException($"{entityId} already has primary body");
         }
         
         // Store component data and state.
         ref var component = ref components.AtIndex(id);
         
         component.EntityId          = entityId;
         component.BodyId            = bodyId;
         component.Primary           = primary;
         component.UserData          = userData;
         component.CurrentContacts  ??= new List<BodyComponentContact>();
         component.LastContacts     ??= new List<BodyComponentContact>();
         component.EnteringContacts ??= new List<BodyComponentContact>();
         component.LeavingContacts  ??= new List<BodyComponentContact>();

         return id;
      }

      public override bool Delete(int id)
      {
         ref var component = ref components.AtIndex(id);
         
         component.CurrentContacts.Clear();
         component.LastContacts.Clear();
         component.EnteringContacts.Clear();
         component.LeavingContacts.Clear();
         
         // Delete world data.           
         world.Delete(component.BodyId);
         
         dirty.Remove(id);
         
         return base.Delete(id);
      }

      public Vector2 GetPosition(int id)
      {
         AssertAlive(id);
         
         return world.Bodies.WithId(components.AtIndex(id).BodyId).Position;
      }

      public float GetRotation(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).Rotation;
      }

      public Vector2 GetActiveLinearForce(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).LinearVelocity;
      }

      public float GetActiveAngularForce(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).AngularVelocity;
      }

      public Vector2 GetNextPosition(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).ForcedPosition;
      }

      public float GetNextRotation(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).ForcedRotation;
      }

      public Aabb GetBoundingBox(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).BoundingBox;
      }

      public BodyType GetType(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).Type;
      }

      public Shape GetShape(int id)
      {
         AssertAlive(id);

         return world.Bodies.WithId(components.AtIndex(id).BodyId).Shape;
      }

      public IEnumerable<BodyComponentContact> GetCurrentContacts(int id)
      {
         AssertAlive(id);

         return components.AtIndex(id).CurrentContacts;
      }

      public IEnumerable<BodyComponentContact> GetLeavingContacts(int id)
      {
         AssertAlive(id);

         return components.AtIndex(id).LeavingContacts;
      }

      public object GetUserData(int id)
      {
         AssertAlive(id);
         
         return components.AtIndex(id).UserData;
      }

      public bool IsPrimary(int id)
      {
         AssertAlive(id);

         return components.AtIndex(id).Primary;
      }

      public void SetPosition(int id, Vector2 position)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).ForcedPosition = position;
         
         UpdateBodyDirtyState(id);
      }

      public void SetRotation(int id, float rotation)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).ForcedRotation = rotation;
         
         UpdateBodyDirtyState(id);
      }

      public void SetLinearVelocity(int id, Vector2 velocity)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).SetLinearVelocity(velocity);
         
         UpdateBodyDirtyState(id);
      }

      public void SetAngularVelocity(int id, float velocity)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).SetAngularVelocity(velocity);
         
         UpdateBodyDirtyState(id);
      }

      public void ApplyLinearImpulse(int id, float magnitude, Vector2 direction)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).ApplyLinearImpulse(magnitude, direction);
         
         UpdateBodyDirtyState(id);
      }

      public void ApplyAngularImpulse(int id, float magnitude)
      {
         AssertAlive(id);
         
         world.Bodies.WithId(components.AtIndex(id).BodyId).ApplyAngularImpulse(magnitude);
         
         UpdateBodyDirtyState(id);
      }

      public void SetUserData(int id, object userData)
      {
         AssertAlive(id);
         
         components.AtIndex(id).UserData = userData;
      }

      public IEnumerable<int> QuerySurroundings(int id, float area)
      {
         AssertAlive(id);
         
         ref var body = ref world.Bodies.WithId(components.AtIndex(id).BodyId);
         
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

      public override void Update(IGameEngineTime time)
      {
         base.Update(time);
         
         if (dirty.Count == 0) return;
         
         foreach (var id in dirty)
         {
            ref var component = ref components.AtIndex(id);
            ref var body      = ref world.Bodies.WithId(components.AtIndex(id).BodyId);
         
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
            
            transforms.ApplyTransformation(transformId, new Transform(body.Position, currentTransform.Scale, body.Rotation));
         }
         
         dirty.Clear();
      }
   }
}