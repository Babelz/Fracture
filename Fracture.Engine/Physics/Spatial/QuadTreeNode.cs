using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Spatial
{
    public delegate bool BodySelector(int bodyId, BodyList bodies);

    /// <summary>
    /// Represents single node inside the quad tree.
    /// </summary>
    public sealed class QuadTreeNode
    {
        #region Fields
        private readonly BodyList bodies;

        private readonly Aabb boundingBox;

        private readonly int bodyLimit;
        private readonly int maxDepth;

        // Body lists that separate bodies based on their type.
        private List<int> [] bodyLists;
        #endregion

        #region Events
        public event EventHandler<BodyEventArgs> Removed;
        public event EventHandler<BodyEventArgs> Added;
        #endregion

        #region Properties
        public QuadTreeNode TopLeft
        {
            get;
            private set;
        }

        public QuadTreeNode TopRight
        {
            get;
            private set;
        }

        public QuadTreeNode BottomLeft
        {
            get;
            private set;
        }

        public QuadTreeNode BottomRight
        {
            get;
            private set;
        }

        public int Depth
        {
            get;
        }

        public bool IsSplit => TopLeft != null; // We can always assume if split has happened, top left is not null.

        public int Count => bodyLists?.Sum(l => l.Count) ?? 0;

        public Aabb BoundingBox => boundingBox;

        public IEnumerable<int> Sensors => bodyLists?[(int)BodyType.Sensor - 1] ?? Enumerable.Empty<int>();

        public IEnumerable<int> Statics => bodyLists?[(int)BodyType.Static - 1] ?? Enumerable.Empty<int>();

        public IEnumerable<int> Dynamics => bodyLists?[(int)BodyType.Dynamic - 1] ?? Enumerable.Empty<int>();
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="QuadTreeNode"/> with
        /// given configuration.
        /// </summary>
        /// <param name="bodies">list containing body data</param>
        /// <param name="bodyLimit">limit of bodies before split occurs</param>
        /// <param name="maxDepth">max depth of the tree in nodes</param>
        /// <param name="depth">current depth of the tree and this node</param>
        /// <param name="center">center position of the node</param>
        /// <param name="bounds">bounds of the node</param>
        public QuadTreeNode(BodyList bodies,
                            int bodyLimit,
                            int maxDepth,
                            int depth,
                            Vector2 center,
                            Vector2 bounds)
        {
            boundingBox = new Aabb(center, bounds * 0.5f);

            this.bodies    = bodies ?? throw new ArgumentNullException(nameof(bodies));
            this.bodyLimit = bodyLimit;
            this.maxDepth  = maxDepth;

            Depth = depth;

            // Create body lists.
            bodyLists = new List<int>[QuadTreeNodeLink.ListsCount];

            for (var i = 0; i < bodyLists.Length; i++)
                bodyLists[i] = new List<int>();
        }

        public QuadTreeNode(BodyList bodies,
                            int bodyLimit,
                            int maxDepth,
                            Vector2 bounds)
            : this(bodies,
                   bodyLimit,
                   maxDepth,
                   0,
                   bounds * 0.5f,
                   bounds)
        {
        }

        private void Split()
        {
            // Split current to 4 cells.
            var bounds = BoundingBox.HalfBounds;

            TopLeft = new QuadTreeNode(
                bodies,
                bodyLimit,
                maxDepth,
                Depth + 1,
                BoundingBox.Position - bounds * 0.5f,
                bounds);

            TopRight = new QuadTreeNode(
                bodies,
                bodyLimit,
                maxDepth,
                Depth + 1,
                TopLeft.BoundingBox.Position + bounds * Vector2.UnitX,
                bounds);

            BottomRight = new QuadTreeNode(
                bodies,
                bodyLimit,
                maxDepth,
                Depth + 1,
                TopRight.BoundingBox.Position + bounds * Vector2.UnitY,
                bounds);

            BottomLeft = new QuadTreeNode(
                bodies,
                bodyLimit,
                maxDepth,
                Depth + 1,
                BottomRight.BoundingBox.Position - bounds * Vector2.UnitX,
                bounds);

            // Delete this nodes body list and transfer to children.
            foreach (var bodyList in bodyLists)
            {
                foreach (var bodyId in bodyList)
                {
                    // Bodies can exist in more than one cell.
                    Debug.Assert(TopLeft.Add(bodyId) || TopRight.Add(bodyId) || BottomRight.Add(bodyId) || BottomLeft.Add(bodyId));
                }
            }

            // Allow GC to collect and compact tree.
            bodyLists = null;
        }

        private void GetActiveBodiesOfType(BodyType type, ICollection<int> moving)
        {
            var bodyList = bodyLists[(int)type - 1];
            var i        = 0;

            while (i < bodyList.Count)
            {
                ref var body = ref bodies.WithId(bodyList[i]);

                // Store bodies out of this nodes bound for reinsertion and skip all duplicates.
                if (body.IsActive() && !moving.Contains(bodyList[i]))
                    moving.Add(bodyList[i]);

                i++;
            }
        }

        public bool Add(int bodyId)
        {
            ref var body = ref bodies.WithId(bodyId);

            // If no collision with this cell, we assume no collision
            // can exist with the children cells even while split.
            if (!Aabb.Intersects(body.BoundingBox, boundingBox))
                return false;

            // If we have reached the static body limit for this cell
            // and depth allows us to split, do that.
            if (!IsSplit && bodyLists.Sum(l => l.Count) >= bodyLimit && Depth < maxDepth)
                Split();

            // Add to self or to children.
            if (IsSplit)
            {
                // Body can exist in multiple nodes.
                var tla = TopLeft.Add(bodyId);
                var tra = TopRight.Add(bodyId);

                var bra = BottomRight.Add(bodyId);
                var bla = BottomLeft.Add(bodyId);

                return tla || tra || bra || bla;
            }
            else
            {
                // Add to self.
                bodyLists[(int)body.Type - 1].Add(bodyId);

                Added?.Invoke(this, new BodyEventArgs(bodyId));
            }

            return true;
        }

        public bool Remove(int bodyId)
        {
            ref var body = ref bodies.WithId(bodyId);

            // If no collision with this cell, we assume no collision
            // can exist with the children cells even while split. 
            // When removing a body, we need to check collision between both 
            // bounding volumes in case the body is active.
            if ((body.IsActive() && !Aabb.Intersects(body.BoundingBox, boundingBox)))
                return false;

            if (IsSplit)
            {
                // Body can exist in multiple nodes.
                var tlr = TopLeft.Remove(bodyId);
                var trr = TopRight.Remove(bodyId);

                var brr = BottomRight.Remove(bodyId);
                var blr = BottomLeft.Remove(bodyId);

                return tlr || trr || brr || blr;
            }

            if (!bodyLists[(int)body.Type - 1].Remove(bodyId))
                return false;

            Removed?.Invoke(this, new BodyEventArgs(bodyId));

            return true;
        }

        public QuadTreeNodeLink RootQuery(QuadTreeNodeLink link)
        {
            if (IsSplit)
            {
                link = TopLeft.RootQuery(link);
                link = TopRight.RootQuery(link);

                link = BottomRight.RootQuery(link);
                link = BottomLeft.RootQuery(link);
            }
            else if (Count != 0)
            {
                link = link.Link(bodyLists);
            }

            return link;
        }

        public QuadTreeNodeLink AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb)
        {
            if (!Aabb.Intersects(aabb, boundingBox))
                return link;

            if (IsSplit)
            {
                link = TopLeft.AabbQueryNarrow(link, aabb);
                link = TopRight.AabbQueryNarrow(link, aabb);

                link = BottomRight.AabbQueryNarrow(link, aabb);
                link = BottomLeft.AabbQueryNarrow(link, aabb);
            }
            else if (Count != 0)
            {
                link = link.Link(bodyLists);
            }

            return link;
        }

        public QuadTreeNodeLink RayCastNarrow(QuadTreeNodeLink link, in Line line)
        {
            if (!Line.Intersects(line, boundingBox))
                return link;

            if (IsSplit)
            {
                link = TopLeft.RayCastNarrow(link, line);
                link = TopRight.RayCastNarrow(link, line);

                link = BottomRight.RayCastNarrow(link, line);
                link = BottomLeft.RayCastNarrow(link, line);
            }
            else if (Count != 0)
            {
                link = link.Link(bodyLists);
            }

            return link;
        }

        public QuadTreeNodeLink AabbQueryBroad(QuadTreeNodeLink link,
                                               in Aabb aabb,
                                               BodySelector selector = null)
        {
            if (!Aabb.Intersects(boundingBox, aabb))
                return link;

            if (IsSplit)
            {
                link = TopLeft.AabbQueryBroad(link, aabb, selector);
                link = TopRight.AabbQueryBroad(link, aabb, selector);

                link = BottomRight.AabbQueryBroad(link, aabb, selector);
                link = BottomLeft.AabbQueryBroad(link, aabb, selector);
            }
            else if (Count != 0)
            {
                link = link.Link(out var results);

                foreach (var bodyList in bodyLists)
                {
                    foreach (var bodyId in bodyList.Where(id => !(!selector?.Invoke(id, bodies) ?? false)))
                    {
                        // All queries should be done using the current 
                        // bounding box, not the future one.
                        ref var body = ref bodies.WithId(bodyId);

                        if (!Aabb.Intersects(body.BoundingBox, aabb))
                            continue;

                        results[(int)body.Type - 1].Add(bodyId);
                    }
                }

                if (results.All(l => l.Count == 0))
                    link = link.Unlink();
            }

            return link;
        }

        public QuadTreeNodeLink RayCastBroad(QuadTreeNodeLink link, in Line line, BodySelector selector = null)
        {
            if (!Line.Intersects(line, boundingBox))
                return link;

            if (IsSplit)
            {
                link = TopLeft.RayCastBroad(link, line, selector);
                link = TopRight.RayCastBroad(link, line, selector);

                link = BottomRight.RayCastBroad(link, line, selector);
                link = BottomLeft.RayCastBroad(link, line, selector);
            }
            else if (Count != 0)
            {
                link = link.Link(out var results);

                foreach (var bodyList in bodyLists)
                {
                    foreach (var bodyId in bodyList.Where(id => !(!selector?.Invoke(id, bodies) ?? false)))
                    {
                        // All queries should be done using the current 
                        // bounding box, not the future one.
                        ref var body = ref bodies.WithId(bodyId);

                        if (!Line.Intersects(line, body.BoundingBox))
                            continue;

                        results[(int)body.Type - 1].Add(bodyId);
                    }
                }

                if (results.All(l => l.Count == 0))
                    link = link.Unlink();
            }

            return link;
        }

        public void GetActiveBodies(ISet<int> moving)
        {
            if (IsSplit)
            {
                // Split, update all children.
                TopLeft.GetActiveBodies(moving);
                TopRight.GetActiveBodies(moving);

                BottomRight.GetActiveBodies(moving);
                BottomLeft.GetActiveBodies(moving);

                // Out of bounds bodies returned by this call should be handled
                // by the top most level that is the tree.
                return;
            }

            // Update dynamics and sensors.
            GetActiveBodiesOfType(BodyType.Dynamic, moving);
            GetActiveBodiesOfType(BodyType.Sensor, moving);
        }
    }
}