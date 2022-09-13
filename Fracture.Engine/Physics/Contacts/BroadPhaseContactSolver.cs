﻿using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Util;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics.Contacts
{
    /// <summary>
    /// Structure that represents a contact pair which contain two bodies of interest for checking collision in 
    /// narrow phase. These are generated by the broad phase contact solver.
    /// </summary>
    public readonly struct ContactPair
    {
        #region Properties
        /// <summary>
        /// Fist body of interest that could collide with the second body.
        /// </summary>
        public int FirstBodyId
        {
            get;
        }

        /// <summary>
        /// Second body of interest that could collide with the first one.
        /// </summary>
        public int SecondBodyId
        {
            get;
        }
        #endregion

        public ContactPair(int firstBodyId, int secondBodyId)
        {
            FirstBodyId  = firstBodyId;
            SecondBodyId = secondBodyId;
        }

        public bool Equals(in ContactPair other)
            => FirstBodyId == other.FirstBodyId &&
               SecondBodyId == other.SecondBodyId;

        public override bool Equals(object obj)
            => obj is ContactPair cp && Equals(cp);

        public override int GetHashCode()
            => HashUtils.Create()
                        .Append(FirstBodyId)
                        .Append(SecondBodyId);

        public static bool operator ==(in ContactPair lhs, in ContactPair rhs)
            => lhs.Equals(rhs);

        public static bool operator !=(in ContactPair lhs, in ContactPair rhs)
            => !lhs.Equals(rhs);
    }

    /// <summary>
    /// Class that handles broad phase contact solving between
    /// bodies based on their current translation and transformation.
    /// </summary>
    public sealed class BroadPhaseContactSolver
    {
        #region Constant fields
        // Initial capacity of contacts in the solver.
        private const int Capacity = 32;
        #endregion

        #region Fields
        private readonly HashSet<ContactPair> lookup;

        private readonly LinearGrowthList<ContactPair> pairs;

        private int pairsCount;
        #endregion

        #region Properties
        public bool ContainsPairs => pairsCount > 0;
        #endregion

        public BroadPhaseContactSolver()
        {
            lookup = new HashSet<ContactPair>();
            pairs  = new LinearGrowthList<ContactPair>(256);
        }

        private bool PairExists(in Body a, in Body b)
            => lookup.Contains(new ContactPair(a.Id, b.Id));

        private void EnqueuePair(Body a, Body b)
        {
            var pair = new ContactPair(a.Id, b.Id);

            pairs.Insert(pairsCount++, pair);

            lookup.Add(pair);
        }

        /// <summary>
        /// Returns next available contact pair.
        /// </summary>
        public ref ContactPair Next()
        {
            ref var pair = ref pairs.AtIndex(--pairsCount);

            lookup.Remove(pair);

            return ref pair;
        }

        private void SolveNode(QuadTreeNode node, float delta)
        {
            if (node.IsSplit)
            {
                SolveNode(node.TopLeft, delta);
                SolveNode(node.TopRight, delta);
                SolveNode(node.BottomRight, delta);
                SolveNode(node.BottomLeft, delta);

                return;
            }

            // Pair all dynamics and statics.
            if (node.Dynamics.Any() && node.Statics.Any())
            {
                foreach (var dynamicBodyId in node.Dynamics)
                {
                    ref var dynamicBody = ref node.Bodies.AtIndex(dynamicBodyId);
                    
                    // If not transformation or translation is being applied to the body,
                    // we can skip bounding volume checks thus eliminating pairing.
                    if (!dynamicBody.Active)
                        continue;

                    foreach (var staticBodyId in node.Statics)
                    {
                        ref var staticBody = ref node.Bodies.AtIndex(staticBodyId);

                        // Does the pair already exist.
                        if (PairExists(dynamicBody, staticBody))
                            continue;

                        // If bounding volumes do not intersect, we can skip
                        // rest of the checks.
                        if (!Aabb.Intersects(staticBody.BoundingBox, dynamicBody.TransformBoundingBox))
                            continue;

                        // These bodies bounding volumes intersect so
                        // we queue them for narrow phase check.
                        EnqueuePair(dynamicBody, staticBody);
                    }
                }
            }

            // Pair all sensors and dynamics.
            if (node.Sensors.Any() && node.Dynamics.Any())
            {
                foreach (var sensorId in node.Sensors)
                {
                    ref var sensorBody = ref node.Bodies.AtIndex(sensorId);

                    // For sensors, we need to check each time.
                    foreach (var dynamicBodyId in node.Dynamics)
                    {
                        ref var dynamicBody = ref node.Bodies.AtIndex(dynamicBodyId);

                        // Does the pair already exist.
                        if (PairExists(sensorBody, dynamicBody))
                            continue;

                        // If bounding volumes do not intersect, we can skip
                        // rest of the checks.
                        if (!Aabb.Intersects(dynamicBody.TransformBoundingBox, sensorBody.TransformBoundingBox))
                            continue;

                        // These bodies bounding volumes intersect so
                        // we queue them for narrow phase check.
                        EnqueuePair(sensorBody, dynamicBody);
                    }
                }
            }
        }

        /// <summary>
        /// Does full broad phase contact solving based on 
        /// the configuration of the solver and generates
        /// appropriate collision pairs.
        /// 
        /// TODO: add threading.
        /// </summary>
        public void Solve(QuadTree tree, float delta)
            => SolveNode(tree.Root, delta);
    }
}