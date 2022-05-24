using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics
{
    /// <summary>
    /// Represents single node inside the quad tree.
    /// </summary>
    public sealed class QuadTreeNode
    {
        #region Fields
        private readonly Aabb boundingBox;

        private readonly int staticBodyLimit;
        private readonly int maxDepth;
        
        // Body lists that separate bodies based on their type.
        private List<Body>[] bodyLists;
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

        public bool IsSplit
            => TopLeft != null; // We can always assume if split has happened, top left is not null.

        public int Count
            => bodyLists?.Sum(l => l.Count) ?? 0;
        
        public Aabb BoundingBox
            => boundingBox;

        public IEnumerable<Body> Sensors
            => bodyLists?[(int)BodyType.Sensor - 1] ?? Enumerable.Empty<Body>();

        public IEnumerable<Body> Statics
            => bodyLists?[(int)BodyType.Static - 1] ?? Enumerable.Empty<Body>();

        public IEnumerable<Body> Dynamics
            => bodyLists?[(int)BodyType.Dynamic - 1] ?? Enumerable.Empty<Body>();
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="QuadTreeNode"/> with
        /// given configuration.
        /// </summary>
        /// <param name="staticBodyLimit">limit static bodies before split occurs</param>
        /// <param name="maxDepth">max depth of the tree in nodes</param>
        /// <param name="depth">current depth of the tree and this node</param>
        /// <param name="center">center position of the node</param>
        /// <param name="bounds">bounds of the node</param>
        public QuadTreeNode(int staticBodyLimit, 
                            int maxDepth, 
                            int depth, 
                            Vector2 center,
                            Vector2 bounds)
        {
            boundingBox = new Aabb(center, bounds * 0.5f);

            this.staticBodyLimit = staticBodyLimit;
            this.maxDepth        = maxDepth;

            Depth = depth;

            // Create body lists.
            bodyLists = new List<Body>[QuadTreeNodeLink.ListsCount];

            for (var i = 0; i < bodyLists.Length; i++)
                bodyLists[i] = new List<Body>();
        }

        public QuadTreeNode(int staticBodyLimit,
                            int maxDepth,
                            Vector2 bounds)
            : this(staticBodyLimit,
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
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                BoundingBox.Position - bounds * 0.5f,
                bounds);

            TopRight = new QuadTreeNode(
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                TopLeft.BoundingBox.Position + bounds * Vector2.UnitX,
                bounds);

            BottomRight = new QuadTreeNode(
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                TopRight.BoundingBox.Position + bounds * Vector2.UnitY,
                bounds);

            BottomLeft = new QuadTreeNode(
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                BottomRight.BoundingBox.Position - bounds * Vector2.UnitX,
                bounds);

            // Delete this nodes body list and transfer to
            // children.
            foreach (var bodies in bodyLists)
            {
                foreach (var body in bodies)
                {
                    // Bodies can exist in more than one cell.
                    var tl = TopLeft.Add(body);
                    var tr = TopRight.Add(body);

                    var br = BottomRight.Add(body);
                    var bl = BottomLeft.Add(body);

                    Debug.Assert(tl || tr || br || bl);
                }
            }

            // Allow GC to collect and compact tree.
            bodyLists = null;
        }

        private void GetActiveBodiesOfType(BodyType type, ICollection<Body> moving)
        {
            var bodies = bodyLists[(int)type - 1];
            var i      = 0;
        
            while (i < bodies.Count)
            {
                // Store bodies out of this nodes bound for reinsertion and skip all duplicates.
                if (!moving.Contains(bodies[i]) && !bodies[i].Active)
                    moving.Add(bodies[i]);
                
                i++;
            }
        }

        public bool Add(Body body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            // If no collision with this cell, we assume no collision
            // can exist with the children cells even while split.
            if (!Aabb.Intersects(body.TransformBoundingBox, boundingBox))
                return false;
            
            // If we have reached the static body limit for this cell
            // and depth allows us to split, do that.
            if (!IsSplit && body.Type == BodyType.Static && bodyLists[(int)BodyType.Static - 1].Count >= staticBodyLimit && Depth < maxDepth)
                Split();

            // Add to self or to children.
            if (IsSplit)
            {
                // Body can exist in multiple nodes.
                var tla = TopLeft.Add(body);
                var tra = TopRight.Add(body);

                var bra = BottomRight.Add(body);
                var bla = BottomLeft.Add(body);

                return tla || tra || bra || bla;
            }
            else
            {
                // Add to self.
                bodyLists[(int)body.Type - 1].Add(body);

                Added?.Invoke(this, new BodyEventArgs(body));
            }

            return true;
        }

        public bool Remove(Body body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            // If no collision with this cell, we assume no collision
            // can exist with the children cells even while split. 
            // When removing a body, we need to check collision between both 
            // bounding volumes in case the body is active.
            if ((body.Active && !Aabb.Intersects(body.TransformBoundingBox, boundingBox)) &&
                 !Aabb.Intersects(body.BoundingBox, boundingBox))
                return false;

            if (IsSplit)
            {
                // Body can exist in multiple nodes.
                var tlr = TopLeft.Remove(body);
                var trr = TopRight.Remove(body);

                var brr = BottomRight.Remove(body);
                var blr = BottomLeft.Remove(body);

                return tlr || trr || brr || blr;
            }

            if (bodyLists[(int)body.Type - 1].Remove(body))
            {
                Removed?.Invoke(this, new BodyEventArgs(body));

                return true;
            }

            return false;
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
            if (!Aabb.Intersects(boundingBox, aabb))
                return link;

            if (IsSplit)
            {
                link = TopLeft.AabbQueryNarrow(link, in aabb);
                link = TopRight.AabbQueryNarrow(link, in aabb);

                link = BottomRight.AabbQueryNarrow(link, in aabb);
                link = BottomLeft.AabbQueryNarrow(link, in aabb);
            }
            else if (Count != 0)
            {
                link = link.Link(bodyLists);
            }

            return link;
        }

        public QuadTreeNodeLink RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b)
        {
            if (!Line.Intersects(new Line(a, b), boundingBox))
                return link;

            if (IsSplit)
            {
                link = TopLeft.RayCastNarrow(link, in a, in b);
                link = TopRight.RayCastNarrow(link, in a, in b);

                link = BottomRight.RayCastNarrow(link, in a, in b);
                link = BottomLeft.RayCastNarrow(link, in a, in b);
            }
            else if (Count != 0)
            {
                link = link.Link(bodyLists);
            }

            return link;
        }

        public QuadTreeNodeLink AabbQueryBroad(QuadTreeNodeLink link, 
                                               in Aabb aabb,
                                               Func<Body, bool> selector = null)
        {
            if (!Aabb.Intersects(boundingBox, aabb))
                return link;

            if (IsSplit)
            {
                link = TopLeft.AabbQueryBroad(link, in aabb, selector);
                link = TopRight.AabbQueryBroad(link, in aabb, selector);

                link = BottomRight.AabbQueryBroad(link, in aabb, selector);
                link = BottomLeft.AabbQueryBroad(link, in aabb, selector);
            }
            else if (Count != 0)
            {
                link = link.Link(out var results);

                foreach (var list in bodyLists)
                {
                    foreach (var body in list)
                    {
                        if (!selector?.Invoke(body) ?? false)
                            continue;

                        // All queries should be done using the current 
                        // bounding box, not the future one.
                        if (!Aabb.Intersects(body.BoundingBox, aabb))
                            continue;

                        results[(int)body.Type - 1].Add(body);
                    }
                }

                if (!results.Any(l => l.Count != 0))
                    link = link.Unlink();
            }

            return link;
        }

        public QuadTreeNodeLink RayCastBroad(QuadTreeNodeLink link, 
                                             in Vector2 a, 
                                             in Vector2 b,
                                             Func<Body, bool> selector = null)
        {
            if (!Line.Intersects(new Line(a, b), boundingBox))
                return link;

            if (IsSplit)
            {
                link = TopLeft.RayCastBroad(link, in a, in b, selector);
                link = TopRight.RayCastBroad(link, in a, in b, selector);

                link = BottomRight.RayCastBroad(link, in a, in b, selector);
                link = BottomLeft.RayCastBroad(link, in a, in b, selector);
            }
            else if (Count != 0)
            {
                link = link.Link(out var results);

                foreach (var list in bodyLists)
                {
                    foreach (var body in list)
                    {
                        if (!selector?.Invoke(body) ?? false)
                            continue;

                        // All queries should be done using the current 
                        // bounding box, not the future one.
                        if (!Line.Intersects(new Line(a, b), body.BoundingBox))
                            continue;

                        results[(int)body.Type - 1].Add(body);
                    }
                }

                if (!results.Any(l => l.Count != 0))
                    link = link.Unlink();
            }

            return link;
        }
        
        public void GetActiveBodies(ISet<Body> moving)
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
