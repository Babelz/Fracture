﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Contacts
{
    /// <summary>
    /// Structure that contains contact information of two bodies. These
    /// are generated by the narrow phase contact solver.
    /// </summary>
    public readonly struct Contact
    {
        #region Properties
        /// <summary>
        /// Id of the first body involved in the collision. Always guaranteed to be
        /// dynamic or sensor.
        /// </summary>
        public int FirstBodyId
        {
            get;
        }

        /// <summary>
        /// Id of the second body involved in the collision. Always guaranteed to be
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
        #endregion

        public Contact(int firstBodyId, int secondBodyId, in Vector2 translation)
        {
            FirstBodyId  = firstBodyId;
            SecondBodyId = secondBodyId;
            Translation  = translation;
        }
    }

    /// <summary>
    /// Class that handles narrow phase contact solving between bodies.
    /// </summary>
    public sealed unsafe class NarrowPhaseContactSolver
    {
        #region Fields
        private readonly bool[][] mask;

        private readonly LinearGrowthList<Contact> contacts;

        private int contactsCount;
        #endregion

        #region Properties
        public bool ContainsContacts => contactsCount > 0;
        #endregion

        public NarrowPhaseContactSolver()
        {
            contacts = new LinearGrowthList<Contact>(256);

            // Generate collision mask, this mask is used to tell us
            // if bodies of given type should collide. Reduce one to keep
            // array tight.
            mask = new bool[Enum.GetValues(typeof(BodyType)).Cast<byte>().Max()][];

            // Static.
            mask[(int)BodyType.Static - 1] = new bool[3];

            mask[(int)BodyType.Static - 1][(int)BodyType.Dynamic - 1] = true;
            mask[(int)BodyType.Static - 1][(int)BodyType.Sensor - 1]  = false;
            mask[(int)BodyType.Static - 1][(int)BodyType.Static - 1]  = false;

            // Dynamic
            mask[(int)BodyType.Dynamic - 1] = new bool[3];

            mask[(int)BodyType.Dynamic - 1][(int)BodyType.Dynamic - 1] = false;
            mask[(int)BodyType.Dynamic - 1][(int)BodyType.Sensor - 1]  = true;
            mask[(int)BodyType.Dynamic - 1][(int)BodyType.Static - 1]  = true;

            // Sensor.
            mask[(int)BodyType.Sensor - 1] = new bool[3];

            mask[(int)BodyType.Sensor - 1][(int)BodyType.Dynamic - 1] = true;
            mask[(int)BodyType.Sensor - 1][(int)BodyType.Sensor - 1]  = false;
            mask[(int)BodyType.Sensor - 1][(int)BodyType.Static - 1]  = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ProjectPolygon(IReadOnlyList<Vector2> points, in Vector2 axis)
        {
            // Set min and max to the first point for a base case.
            var projection = new Vector2(Vector2.Dot(axis, points[0]));

            for (var i = 1; i < points.Count; i++)
            {
                var next = Vector2.Dot(axis, points[i]);

                if (next < projection.X)
                    projection.X = next;
                else if (next > projection.Y)
                    projection.Y = next;
            }

            return projection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ProjectCircle(float radius, IReadOnlyList<Vector2> points, in Vector2 axis)
        {
            var projection = Vector2.Dot(axis, points[0]);

            return new Vector2(projection - radius, projection + radius);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ProjectionsOverlap(in Vector2 a, in Vector2 b)
            => a.Y >= b.X || a.X >= b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ComputeProjectionOverlap(in Vector2 a, in Vector2 b)
            => Math.Min(a.Y, b.Y) - Math.Max(a.X, b.X);

        private void EnqueueContact(in Body a, in Body b, in Vector2 translation)
        {
            if (a.Type == BodyType.Sensor || a.Type == BodyType.Dynamic)
                contacts.Insert(contactsCount++, new Contact(a.Id, b.Id, translation));
            else
                contacts.Insert(contactsCount++, new Contact(b.Id, a.Id, translation));
        }

        #region Solvers
        private bool SolvePolygonToPolygon(in Body a, in Body b)
        {
            Debug.Assert(a.Shape.Type == ShapeType.Polygon);
            Debug.Assert(b.Shape.Type == ShapeType.Polygon);

            // Solver should have done this right, but for some reason not?
            if (!Aabb.Intersects(a.BoundingBox, b.BoundingBox))
                return false;

            // Get points to polygons.
            var ap = a.Shape.WorldVertices;
            var bp = b.Shape.WorldVertices;

            // If no points, return.
            if (ap.Count < 1 || bp.Count < 1)
                return false;

            // Minimum translation axis.
            var mta = Vector2.Zero;

            // Minimum translation value. When united with minimum translation axis, we get minimum (response) translation vector.
            var mt = float.MaxValue;

            // Overlap between projections.
            float ol;

            // Get axes to check for each shape.
            var ax = a.Shape.Axes;
            var bx = b.Shape.Axes;

            // Check for a-b overlaps. Project a and b to a axes.
            for (var i = 0; i < ax.Count; i++)
            {
                // Get projections.
                var cax = ax[i];
                var apj = ProjectPolygon(ap, in cax);
                var bpj = ProjectPolygon(bp, in cax);

                // Check for overlap. If no overlap, then no interaction between polygons so we can return,
                // otherwise we compare the minimum translation.
                if (!ProjectionsOverlap(in apj, in bpj))
                    return false;

                // Get value of overlap.
                ol = ComputeProjectionOverlap(in apj, in bpj);

                // If it is less than previous, change mt and axis accordingly.
                if (!(ol < mt))
                    continue;

                mt  = ol;
                mta = ax[i];
            }

            // Check for b-a overlaps. Project a and b to b axes.
            for (var i = 0; i < bx.Count; i++)
            {
                // Get projection.
                var cax = bx[i];
                var apj = ProjectPolygon(ap, in cax);
                var bpj = ProjectPolygon(bp, in cax);

                if (!ProjectionsOverlap(in apj, in bpj))
                    return false;

                // Get value of overlap.
                ol = ComputeProjectionOverlap(in apj, in bpj);

                // If it is less than previous, change mt and axis accordingly.
                if (!(ol < mt))
                    continue;

                mt  = ol;
                mta = bx[i];
            }

            // Sensor optimization, translation is irrelevant here.
            if (a.Type == BodyType.Sensor || b.Type == BodyType.Sensor)
            {
                EnqueueContact(a, b, Vector2.Zero);

                return true;
            }

            // If we got this far then the objects are intersecting and we have the mtv for handling
            // separation between the objects. 
            var dv = a.Position - b.Position;

            // Minimum translation vector for separation.
            // Check side, are we colliding from right or left.
            if (Vector2.Dot(mta, dv) < 0.0f)
            {
                mta.X = -mta.X;
                mta.Y = -mta.Y;
            }

            // Queue new contact for later handling.
            EnqueueContact(a, b, mta * mt);

            return true;
        }

        private bool SolvePolygonToCircle(in Body circle, in Body polygon)
        {
            Debug.Assert(circle.Shape.Type == ShapeType.Circle);
            Debug.Assert(polygon.Shape.Type == ShapeType.Polygon);

            // Minimum translation axis.
            var mta = Vector2.Zero;

            // Minimum translation value. When united with minimum translation axis, we get minimum (response) translation vector.
            var mt = float.MaxValue;

            // Minimum penetration vertex.
            var mv = new Vector2(float.MaxValue);

            // Circle position, center.
            var cp = circle.Position;

            // Circle radius.
            var cr = circle.Shape.BoundingBox.Right - circle.Shape.BoundingBox.Position.X;

            // Polygon world vertices and axes.
            var pv = polygon.Shape.WorldVertices;

            // Copy axes to this span and allocate n + 1.
            var ax = stackalloc Vector2[polygon.Shape.Axes.Count + 1];

            for (var i = 0; i < polygon.Shape.Axes.Count; i++)
                ax[i] = polygon.Shape.Axes[i];

            // Find minimum penetrating vertex. 
            for (var i = 0; i < pv.Count; i++)
            {
                // Distance from vertex to center.
                var d = pv[i] - cp;

                if (d.LengthSquared() < mv.LengthSquared())
                    mv = d;
            }

            // Normalize and make perpendicular.
            mv.Normalize();

            ax[polygon.Shape.Axes.Count] = mv;

            // Check intersection along all axes.
            for (var i = 0; i < polygon.Shape.Axes.Count + 1; i++)
            {
                // Get projections.
                var cax = ax[i];
                var ppj = ProjectPolygon(polygon.Shape.WorldVertices, in cax);
                var cpj = ProjectCircle(cr, circle.Shape.WorldVertices, in cax);

                if (!ProjectionsOverlap(in ppj, in cpj))
                    return false;

                var ol = ComputeProjectionOverlap(in ppj, in cpj);

                if (!(ol < mt))
                    continue;

                mt  = ol;
                mta = ax[i];
            }

            // Sensor optimization, translation is irrelevant here.
            if (circle.Type == BodyType.Sensor || polygon.Type == BodyType.Sensor)
            {
                EnqueueContact(circle, polygon, Vector2.Zero);

                return true;
            }

            // If minimum separation is too small, just 
            // plain ignore all collisions to prevent 
            // body snapping.
            if (mt < 0.005f)
                return false;

            // Ensure axis orientation is pointing away.
            var dv = polygon.Position - circle.Position;

            if (Vector2.Dot(dv, mta) >= 0.0f)
                mta = Vector2.Negate(mta);

            EnqueueContact(circle, polygon, mta * mt);

            return true;
        }

        private bool SolveCircleToCircle(in Body a, in Body b)
        {
            Debug.Assert(a.Shape.Type == ShapeType.Circle);
            Debug.Assert(b.Shape.Type == ShapeType.Circle);

            // Distance between circles and the length.
            var distance = a.Position - b.Position;

            // Radius sum. 
            var rs = a.Shape.BoundingBox.Right - a.Shape.BoundingBox.Position.X + (b.Shape.BoundingBox.Right - b.Shape.BoundingBox.Position.X);

            // Test if two circles collide.
            if (distance.LengthSquared() > rs * rs)
                return false;

            // Sensor optimization, translation is irrelevant here.
            if (a.Type == BodyType.Sensor || b.Type == BodyType.Sensor)
            {
                EnqueueContact(a, b, Vector2.Zero);

                return true;
            }

            // If we get this far, collision occurs, solve that collisions.
            var normal = Vector2.Normalize(distance);

            // Minimum translation to separate the circles.
            EnqueueContact(a, b, rs * normal - distance);

            return true;
        }
        #endregion

        /// <summary>
        /// Returns next available contact.
        /// </summary>
        public ref Contact Next()
            => ref contacts.AtIndex(--contactsCount);

        /// <summary>
        /// Attempts to solve collision between to bodies in narrow phase manner. 
        /// In case the bodies do collide, the solver will store this as an 
        /// contact for later use. Returns boolean whether the two bodies actually collided. 
        /// </summary>
        public bool Solve(in Body a, in Body b)
        {
            // Check body type mask for possible collision. No need
            // to do AABB (broad phase) checks as this should be done
            // before doing narrow phase.
            if (!mask[(int)a.Type - 1][(int)b.Type - 1])
                return false;

            if (a.Shape.Type == ShapeType.Circle)
                return b.Shape.Type == ShapeType.Polygon ? SolvePolygonToCircle(a, b) : SolveCircleToCircle(a, b);

            return b.Shape.Type == ShapeType.Polygon ? SolvePolygonToPolygon(a, b) : SolveCircleToCircle(b, a);
        }
    }
}