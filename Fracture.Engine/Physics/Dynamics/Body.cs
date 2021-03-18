using System;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
   /// <summary>
   /// Event arguments for single body events. Use structure to ease memory pressure when creating the events.
   /// </summary>
   public readonly struct BodyEventArgs
   {
      #region Properties
      public int BodyId
      {
         get;
      } 
      #endregion

      public BodyEventArgs(int bodyId)
         => BodyId = bodyId;
   }
   
   /// <summary>
   /// Enumeration defining supported body types.
   /// </summary>
   public enum BodyType : byte
   {
      /// <summary>
      /// Undefined body type.
      /// </summary>
      None = 0,

      /// <summary>
      /// Static bodies are moved by movers and collide with dynamic bodies.
      /// </summary>
      Static = 1,

      /// <summary>
      /// Dynamic bodies are moved by their velocity or position and collide with static bodies
      /// and respond to collisions.
      /// </summary>
      Dynamic = 2,

      /// <summary>
      /// Sensor bodies are moved by their velocity or position and only detect collisions
      /// with dynamic bodies but do not respond to them.
      /// </summary>
      Sensor = 3
   }

   /// <summary>
   /// Structure that defines a body. Body properties are based
   /// on fixtures and their geometry is defined by shapes.
   ///
   /// TODO: bodies do not accelerate, fix this in future. Add other forces
   ///       such as wind and friction also if needed.
   ///
   /// TODO: take Virlet or some other integration method to use.
   ///
   /// TODO: currently has no fixture support, add this with additional forces
   ///       and improvements.
   /// </summary>
   public struct Body
   {
      #region Fields
      /// <summary>
      /// Gets or sets the linear force of the body for current frame.
      /// </summary>
      public Vector2 LinearVelocity;
      
      /// <summary>
      /// Gets or sets the angular force of the body for current frame.
      /// </summary>
      public float AngularVelocity;

      /// <summary>
      /// Gets the id of the body.
      /// </summary>
      public readonly int Id;

      /// <summary>
      /// Gets or sets the position of the body. Make sure
      /// to update vertices and AABB if you modify this value.
      /// </summary>
      public Vector2 Position;

      /// <summary>
      /// Gets or sets the next position set for the body.
      /// Use this to force the rotation to given value during next frame.
      /// </summary>
      public Vector2 ForcedPosition;
      
      /// <summary>
      /// Gets or sets the rotation of the body. Make sure to
      /// update vertices and AABB if you modify this value.
      /// </summary>
      public float Rotation;

      /// <summary>
      /// Gets or sets the next rotation value of the body.
      /// Use this to force the rotation to given value during next frame.
      /// </summary>
      public float ForcedRotation;

      /// <summary>
      /// Gets or sets the AABB of the body. Update AABB when
      /// updating body transform.
      /// </summary>
      public Aabb BoundingBox;

      /// <summary>
      /// Gets the type of the body.
      /// </summary>
      public readonly BodyType Type;

      /// <summary>
      /// Gets the vertices of this body. These vertices are
      /// in body space local to the position of the body.
      /// </summary>
      public readonly Vector2[] Vertices;

      /// <summary>
      /// Gets the shape of the body.
      /// </summary>
      public readonly Shape Shape;

      /// <summary>
      /// Gets optional user data of the body.
      /// </summary>
      public object UserData;
      #endregion

      public Body(int id,
                  BodyType type,
                  in Vector2 position, 
                  float rotation, 
                  in Shape shape, 
                  object userData = null) 
      {
         Id       = id;
         Type     = type;
         Position = position;
         Shape    = shape;
         Rotation = rotation;
         Shape    = shape;
         UserData = userData;
         
         LinearVelocity  = Vector2.Zero;
         AngularVelocity = 0.0f;
         
         Vertices    = new Vector2[Shape.Vertices.Length];
         BoundingBox = new Aabb(Position, Rotation, Vertices);
         
         ForcedPosition = Vector2.Zero;
         ForcedRotation = 0.0f;
         
         TransformAngularForces();
      }
      
      /// <summary>
      /// Applies body vertex transform based on linear forces.
      /// </summary>
      public void TransformLinearForces()
      {
         for (var i = 0; i < Vertices.Length; i++) 
            Vertices[i] = Shape.Vertices[i] + Position;
      }
      
      /// <summary>
      /// Applies body vertex transforms based on angular forces.
      /// </summary>
      public void TransformAngularForces()
      {
         var sin = (float)Math.Sin(Rotation);
         var cos = (float)Math.Cos(Rotation);
                
         for (var i = 0; i < Vertices.Length; i++)
         {
            var transform = new Vector2(
               cos * Shape.Vertices[i].X - sin * Shape.Vertices[i].Y, 
               sin * Shape.Vertices[i].X + cos * Shape.Vertices[i].Y
            );

            Vertices[i] = Position + transform;
         }
      }
      
      /// <summary>
      /// Applies linear forces if there are any active. Returns boolean declaring
      /// if any forces were applies.
      /// </summary>
      public bool ApplyLinearForces(IPhysicsSimulationTime time)
      {
         if (LinearVelocity == Vector2.Zero || ForcedPosition == Vector2.Zero)
            return false;
         
         if (ForcedPosition != Vector2.Zero)
            Position = ForcedPosition;
         
         Position += LinearVelocity * (float)time.Elapsed.TotalMilliseconds;
         
         return true;
      }
      
      /// <summary>
      /// Applies angular forces if there are any active. Returns boolean declaring
      /// if any forces were applied.
      /// </summary>
      public bool ApplyAngularForces(IPhysicsSimulationTime time)
      {
         if (AngularVelocity == 0.0f || ForcedRotation == 0.0f)
            return false;
         
         if (ForcedRotation != 0.0f)
            Rotation = ForcedRotation;
         
         Rotation += AngularVelocity * (float)time.Elapsed.TotalSeconds;
         
         return true;
      }

      /// <summary>
      /// Resets all active forces from the body.
      /// </summary>
      public void ResetForces()
      {
         ForcedPosition = Vector2.Zero;
         LinearVelocity = Vector2.Zero;
         
         AngularVelocity   = 0.0f;
         ForcedRotation = 0.0f;
      }
      
      /// <summary>
      /// Forcefully sets the linear velocity of the body.
      /// </summary>
      /// <param name="velocity">velocity in meters/second</param>
      public void SetLinearVelocity(Vector2 velocity)
      {
         LinearVelocity = Vector2.Zero;
         
         ApplyLinearImpulse(0.0f, Vector2.Zero);
      }
      
      /// <summary>
      /// Forcefully sets the angular velocity of the body. 
      /// </summary>
      /// <param name="velocity">velocity in rad/second</param>
      public void SetAngularVelocity(float velocity)
      {
         AngularVelocity = 0.0f;
         
         ApplyAngularImpulse(velocity);
      }
      
      /// <summary>
      /// Applies given impulse to body linear force. Resulting velocity applies
      /// is affected by mass of the body.
      /// </summary>
      /// <param name="magnitude">magnitude of the impulse in meters/second</param>
      /// <param name="direction">unit vector of the direction of the impulse</param>
      public void ApplyLinearImpulse(float magnitude, Vector2 direction)
      {
         LinearVelocity += magnitude * direction;
      }
      
      /// <summary>
      /// Applies given impulse to body angular force. Resulting velocity applies
      /// is affected by mass of the body.
      /// </summary>
      /// <param name="magnitude">magnitude of the in radians/second</param>
      public void ApplyAngularImpulse(float magnitude)
      {
         AngularVelocity += Math.Max(BoundingBox.HalfBounds.X, BoundingBox.HalfBounds.Y) * magnitude;
      }

      public bool IsActive()
         => LinearVelocity != Vector2.Zero || AngularVelocity != 0.0f;
   }
}