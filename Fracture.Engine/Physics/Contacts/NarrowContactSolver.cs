using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Contacts
{
   /// <summary>
   /// Structure contains narrow contact solver results.
   /// </summary>
   public readonly struct NarrowContactSolverResult
   {
      #region Properties
      /// <summary>
      /// Gets the id first body involved in the collision. Always guaranteed to be
      /// dynamic or sensor.
      /// </summary>
      public int FirstBodyId
      {
         get;
      }

      /// <summary>
      /// Gets the id second body involved in the collision. Always guaranteed to be
      /// static of dynamic.
      /// </summary>
      public int SecondBodyId
      {
         get;
      }

      /// <summary>
      /// Translation required to separate bodies from each other.
      /// </summary>
      public Vector2 Translation
      {
         get;
      }

      /// <summary>
      /// Gets the boolean declaring whether and collision actually occurred.
      /// </summary>
      public bool Collides
      {
         get;
      }
      #endregion

      private NarrowContactSolverResult(int firstBodyId, int secondBodyId, Vector2 translation, bool collides)
      {
         FirstBodyId  = firstBodyId;
         SecondBodyId = secondBodyId;
         Translation  = translation;
         Collides     = collides;
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult Collision(int firstBodyId, int secondBodyId, Vector2 translation)
         => new NarrowContactSolverResult(firstBodyId, secondBodyId, translation, true);

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult Separation()
         => new NarrowContactSolverResult(0, 0, Vector2.Zero, false);
   }

   public static unsafe class NarrowContactSolver
   {
      /// <summary>
      /// Attempts to solve contact between two bodies. If no solver is found
      /// for the shape types provided, no contact is returned.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult Solve(in Body a, in Body b)
      {
         // Poly to poly.
         if (a.Shape.IsPolygon() && b.Shape.IsPolygon())
            return SolvePolygonToPolygon(a, b);
         
         // Circle to circle.
         if (a.Shape.IsCircle() && b.Shape.IsCircle())
            return SolveCircleToCircle(a, b);
         
         // Poly to circle.
         if ((a.Shape.IsPolygon() || b.Shape.IsPolygon()) && (a.Shape.IsCircle() || b.Shape.IsCircle()))
            return SolvePolygonToCircle(a, b);
         
         return NarrowContactSolverResult.Separation();
      }
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult SolvePolygonToPolygon(in Body a, in Body b)
      {
#if DEBUG
         if (!a.Shape.IsPolygon() || !b.Shape.IsPolygon())
            throw new InvalidEnumArgumentException("expecting two polygons")
            {
               Data = { { "a", a }, { "b", b } }
            };
#endif
         if (!Aabb.Intersects(a.BoundingBox, b.BoundingBox))
            return NarrowContactSolverResult.Separation();

         // If no points, return.
         if (a.Vertices.Length < 1 || b.Vertices.Length < 1)
            return NarrowContactSolverResult.Separation();

         // Minimum translation axis.
         var mta = Vector2.Zero;

         // Minimum translation value. When united with minimum translation axis, we get minimum (response) translation vector.
         var mt = float.MaxValue;

         // Overlap between projections.
         float ol;

         // Check for a-b overlaps. Project a and b to a axes.
         for (var i = 0; i < a.Shape.Axes.Length; i++)
         {
            // Get projections.
            var cax = a.Shape.Axes[i];
            var apj = Project.Polygon(a.Vertices, cax);
            var bpj = Project.Polygon(b.Vertices, cax);

            // Check for overlap. If no overlap, then no interaction between polygons so we can return,
            // otherwise we compare the minimum translation.
            if (!Project.Overlap(apj, bpj))
               return NarrowContactSolverResult.Separation();

            // Get value of overlap.
            ol = Project.OverlapAmount(apj, bpj);

            // If it is less than previous, change mt and axis accordingly.
            if (!(ol < mt))
               continue;

            mt  = ol;
            mta = a.Shape.Axes[i];
         }

         // Check for b-a overlaps. Project a and b to b axes.
         for (var i = 0; i < b.Shape.Axes.Length; i++)
         {
            // Get projection.
            var cax = b.Shape.Axes[i];
            var apj = Project.Polygon(a.Vertices, cax);
            var bpj = Project.Polygon(b.Vertices, cax);

            if (!Project.Overlap(apj, bpj))
               return NarrowContactSolverResult.Separation();

            // Get value of overlap.
            ol = Project.OverlapAmount(apj, bpj);

            // If it is less than previous, change mt and axis accordingly.
            if (!(ol < mt))
               continue;

            mt  = ol;
            mta = b.Shape.Axes[i];
         }

         // Sensor optimization, translation is irrelevant here.
         if (a.Type == BodyType.Sensor || b.Type == BodyType.Sensor)
            return NarrowContactSolverResult.Collision(a.Id, b.Id, Vector2.Zero);

         // If we got this far then the objects are intersecting and we have the mtv for handling
         // separation between the objects. 
         var dv = a.Position - b.Position;

         // Minimum translation vector for separation.
         // Check side, are we colliding from right or left.
         if (!(Vector2.Dot(mta, dv) < 0.0f))
            return NarrowContactSolverResult.Collision(a.Id, b.Id, mta * mt);

         mta.X = -mta.X;
         mta.Y = -mta.Y;

         // Return new contact for later handling.
         return NarrowContactSolverResult.Collision(a.Id, b.Id, mta * mt);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult SolvePolygonToCircle(in Body a, in Body b)
      {
#if DEBUG
         if (!(!a.Shape.IsPolygon() || !b.Shape.IsPolygon()) || !(!a.Shape.IsCircle() || b.Shape.IsCircle()))
            throw new InvalidOperationException("expecting one circle and one polygon")
            {
               Data = { { "a", a }, { "b", b } }
            };
#endif
         // Solve polygon and circle where p is polygon 
         // and c is circle.
         var polygon = a.Shape.IsPolygon() ? a : b;
         
         var circle = b.Shape.IsCircle() ? a : b;

         // Minimum translation axis.
         var mta = Vector2.Zero;

         // Minimum translation value. When united with minimum translation axis, we get minimum (response) translation vector.
         var mt = float.MaxValue;

         // Minimum penetration vertex.
         var mv = Vector2.Zero;

         // Circle position, center.
         var cp = circle.Position;

         // Circle radius.
         var cr = circle.Shape.Radius();

         // Polygon world vertices and axes.
         var pv = polygon.Vertices;

         // Copy axes to this span and allocate n + 1.
         var al = polygon.Shape.Axes.Length + 1;
         var ax = stackalloc Vector2[al];

         for (var i = 0; i < al - 1; i++)
            ax[i] = polygon.Shape.Axes[i];

         // Find minimum penetrating vertex. 
         for (var i = 0; i < pv.Length; i++)
         {
            // Distance from vertex to center.
            var d = pv[i] - cp;

            if (mv == Vector2.Zero || d.LengthSquared() < mv.LengthSquared())
               mv = d;
         }

         // Normalize and make perpendicular.
         mv.Normalize();

         ax[al - 1] = mv;

         // Check intersection along all axes.
         for (var i = 0; i < al; i++)
         {
            // Get projections.
            var cax = ax[i];
            var ppj = Project.Polygon(polygon.Vertices, cax);
            var cpj = Project.Circle(cr, circle.Vertices, cax);

            if (!Project.Overlap(ppj, cpj))
               return NarrowContactSolverResult.Separation();

            var ol = Project.OverlapAmount(ppj, cpj);

            if (!(ol < mt))
               continue;

            mt  = ol;
            mta = ax[i];
         }

         // Sensor optimization, translation is irrelevant here.
         if (a.Type == BodyType.Sensor || b.Type == BodyType.Sensor)
            return NarrowContactSolverResult.Collision(a.Id, b.Id, Vector2.Zero);

         // If minimum separation is too small, just 
         // plain ignore all collisions to prevent 
         // body snapping.
         if (mt < 0.005f)
            return NarrowContactSolverResult.Separation();

         // Ensure axis orientation is pointing away.
         var dv = b.Position - a.Position;

         if (Vector2.Dot(dv, mta) >= 0.0f)
            mta = Vector2.Negate(mta);

         return NarrowContactSolverResult.Collision(a.Id, b.Id, mta * mt);
      }

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static NarrowContactSolverResult SolveCircleToCircle(in Body a, in Body b)
      {
#if DEBUG
         if (!a.Shape.IsCircle() && !b.Shape.IsCircle())
            throw new InvalidOperationException("expecting two circles")
            {
               Data = { { "a", a }, { "b", b } }
            };
#endif
         // Distance between circles and the length.
         var distance = a.Position - b.Position;

         // Radius sum. 
         var ar = a.Shape.Radius();
         var br = b.Shape.Radius();
         var rs = ar + br;

         // Test if two circles collide.
         if (distance.LengthSquared() > rs * rs)
            return NarrowContactSolverResult.Separation();

         // Sensor optimization, translation is irrelevant here.
         if (a.Type == BodyType.Sensor || b.Type == BodyType.Sensor)
            return NarrowContactSolverResult.Collision(a.Id, b.Id, Vector2.Zero);

         // If we get this far, collision occurs, solve that collisions.
         var normal = Vector2.Normalize(distance);

         // Minimum translation to separate the circles.
         return NarrowContactSolverResult.Collision(a.Id, b.Id, rs * normal - distance);
      }
   }
   
   /// <summary>
   /// List containing last active contacts for single body.
   /// </summary>
   public sealed class ContactList
   {
       #region Constant fields
       private const int Capacity = 32;
       #endregion

       #region Fields
       private HashSet<int> oldBodyIds;
       private HashSet<int> newBodyIds;

       private ulong lastFrame;
       #endregion

       #region Properties
       /// <summary>
       /// Returns old, leaving contacts.
       /// </summary>
       public IEnumerable<int> LeavingBodyIds
         => oldBodyIds.Where(oldContact => !newBodyIds.Contains(oldContact));
       
       /// <summary>
       /// Returns new, entering contacts.
       /// </summary>
       public IEnumerable<int> EnteringBodyIds
         => newBodyIds.Where(newContact => oldBodyIds.Contains(newContact));
           
       /// <summary>
       /// Returns current contacts.
       /// </summary>
       public IEnumerable<int> CurrentBodyIds
           => oldBodyIds;

       /// <summary>
       /// Body that owns this contact list.
       /// </summary>
       public int BodyId
       {
           get;
       }
       #endregion

       public ContactList(int bodyId)
       {
           BodyId     = bodyId;
           oldBodyIds = new HashSet<int>(Capacity);
           newBodyIds = new HashSet<int>(Capacity);
       }
       
       /// <summary>
       /// Adds new body to contact list.
       /// </summary>
       public void Add(int id, ulong frame)
       {
           if (lastFrame < frame)
           {
               // Swap and clear.
               var temp = oldBodyIds;

               oldBodyIds = newBodyIds;
               newBodyIds = temp;

               newBodyIds.Clear();

               lastFrame = frame;
           }

           newBodyIds.Add(id);
       }
   }
}