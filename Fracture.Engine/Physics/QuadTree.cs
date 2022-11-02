using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Events;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics
{
    /// <summary>
    /// Class that represents query results to quad tree as 
    /// linked structure. Creating instance of this class can be
    /// quite heavy, so prefer re-using it as required.
    /// </summary>
    public sealed class QuadTreeNodeLink : IClearable, ICloneable<QuadTreeNodeLink>
    {
        #region Static fields
        public static readonly int ListsCount = Enum.GetValues(typeof(BodyType)).Cast<byte>().Max();
        #endregion

        #region Fields
        private readonly CollectionPool<List<int>>      listPool;
        private readonly DelegatePool<QuadTreeNodeLink> linkPool;
        private readonly ArrayPool<List<int>>           arrayPool;

        /// <summary>
        /// Last node linked.
        /// </summary>
        private QuadTreeNodeLink previous;

        // Current nodes body lists.
        private List<int>[] bodyLists;

        // Does this link own body lists.
        private bool owner;
        #endregion

        #region Properties
        /// <summary>
        /// Next link.
        /// </summary>
        public QuadTreeNodeLink Next
        {
            get;
            private set;
        }

        public IEnumerable<int> Sensors => bodyLists[(int)BodyType.Sensor - 1];

        public IEnumerable<int> Statics => bodyLists[(int)BodyType.Static - 1];

        public IEnumerable<int> Dynamics => bodyLists[(int)BodyType.Dynamic - 1];

        public IEnumerable<int> Bodies => Sensors.Concat(Statics).Concat(Dynamics);

        /// <summary>
        /// Returns boolean declaring whether this is the 
        /// last node in this link.
        /// </summary>
        public bool End => Next == null || bodyLists == null;
        #endregion

        public QuadTreeNodeLink()
        {
            linkPool = new DelegatePool<QuadTreeNodeLink>(
                new LinearStorageObject<QuadTreeNodeLink>(
                    new LinearGrowthArray<QuadTreeNodeLink>(16, 1)),
                Clone);

            listPool = new CollectionPool<List<int>>(
                new DelegatePool<List<int>>(
                    new LinearStorageObject<List<int>>(
                        new LinearGrowthArray<List<int>>(
                            8,
                            1)),
                    () => new List<int>()));

            arrayPool = new ArrayPool<List<int>>(
                () => new LinearStorageObject<List<int>[]>(new LinearGrowthArray<List<int>[]>(8, 1)),
                8);
        }

        private QuadTreeNodeLink(DelegatePool<QuadTreeNodeLink> linkPool,
                                 CollectionPool<List<int>> listPool,
                                 ArrayPool<List<int>> arrayPool)
        {
            this.linkPool  = linkPool;
            this.listPool  = listPool;
            this.arrayPool = arrayPool;
        }

        /// <summary>
        /// Creates and links new node to this link.
        /// </summary>
        public QuadTreeNodeLink Link(List<int>[] bodies)
        {
            bodyLists = bodies;

            // Create next link.
            Next = linkPool.Take();

            Next.previous = this;

            return Next;
        }

        /// <summary>
        /// Creates and links new node to this and allocates
        /// body list for this node and returns it to the caller.
        /// </summary>
        public QuadTreeNodeLink Link(out List<int>[] bodyLists)
        {
            owner     = true;
            bodyLists = arrayPool.Take(ListsCount);

            for (var i = 0; i < bodyLists.Length; i++)
                bodyLists[i] = listPool.Take();

            // Link.
            return Link(bodyLists);
        }

        /// <summary>
        /// Unlinks current node and links to previous.
        /// </summary>
        public QuadTreeNodeLink Unlink()
        {
            if (previous == null)
                return this;

            // Store for later as previous gets cleared.
            var last = previous;

            // Unlink reference before clearing to
            // avoid last link clearing.
            previous = null;

            Clear();

            return last;
        }

        public void Clear()
        {
            // Clear next.
            if (Next != null)
            {
                Next.Clear();

                linkPool.Return(Next);
            }

            // Clear last.
            previous?.Clear();

            // Return resources.
            if (owner)
            {
                for (var i = 0; i < bodyLists.Length; i++)
                    listPool.Return(bodyLists[i]);

                arrayPool.Return(bodyLists);

                owner = false;
            }

            // Reset state.
            Next      = null;
            previous  = null;
            bodyLists = null;
        }

        /// <summary>
        /// Returns deep copy of this link.
        /// </summary>
        public QuadTreeNodeLink Clone()
            => new QuadTreeNodeLink(linkPool, listPool, arrayPool);
    }

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
        private List<int>[] bodyLists;
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

        public BodyList Bodies
        {
            get;
        }
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
        public QuadTreeNode(BodyList bodies,
                            int staticBodyLimit,
                            int maxDepth,
                            int depth,
                            Vector2 center,
                            Vector2 bounds)
        {
            Bodies = bodies ?? throw new ArgumentNullException(nameof(bodies));

            boundingBox = new Aabb(center, bounds * 0.5f);

            this.staticBodyLimit = staticBodyLimit;
            this.maxDepth        = maxDepth;

            Depth = depth;

            // Create body lists.
            bodyLists = new List<int>[QuadTreeNodeLink.ListsCount];

            for (var i = 0; i < bodyLists.Length; i++)
                bodyLists[i] = new List<int>();
        }

        public QuadTreeNode(BodyList bodies,
                            int staticBodyLimit,
                            int maxDepth,
                            Vector2 bounds)
            : this(bodies,
                   staticBodyLimit,
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
                Bodies,
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                BoundingBox.Position - bounds * 0.5f,
                bounds);

            TopRight = new QuadTreeNode(
                Bodies,
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                TopLeft.BoundingBox.Position + bounds * Vector2.UnitX,
                bounds);

            BottomRight = new QuadTreeNode(
                Bodies,
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                TopRight.BoundingBox.Position + bounds * Vector2.UnitY,
                bounds);

            BottomLeft = new QuadTreeNode(
                Bodies,
                staticBodyLimit,
                maxDepth,
                Depth + 1,
                BottomRight.BoundingBox.Position - bounds * Vector2.UnitX,
                bounds);

            // Delete this nodes body list and transfer to
            // children.
            foreach (var bodyList in bodyLists)
            foreach (var bodyId in bodyList)
            {
                ref var body = ref Bodies.AtIndex(bodyId);

                // Bodies can exist in more than one cell.
                var tl = TopLeft.Add(body);
                var tr = TopRight.Add(body);

                var br = BottomRight.Add(body);
                var bl = BottomLeft.Add(body);

                if (!(tl || tr || br || bl))
                    throw new InvalidOperationException($"could not add body {bodyId} to any nodes after split");
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
                // Store bodies out of this nodes bound for reinsertion and skip all duplicates.
                if (!moving.Contains(bodyList[i]) && !Bodies.AtIndex(bodyList[i]).Active)
                    moving.Add(bodyList[i]);

                i++;
            }
        }

        public bool Add(in Body body)
        {
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
                bodyLists[(int)body.Type - 1].Add(body.Id);

                Added?.Invoke(this, new BodyEventArgs(body.Id));
            }

            return true;
        }

        public bool Remove(in Body body)
        {
            // If no collision with this cell, we assume no collision
            // can exist with the children cells even while split. 
            // When removing a body, we need to check collision between both 
            // bounding volumes in case the body is active.
            if (body.Active &&
                !Aabb.Intersects(body.TransformBoundingBox, boundingBox) &&
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

            if (!bodyLists[(int)body.Type - 1].Remove(body.Id))
                return false;

            Removed?.Invoke(this, new BodyEventArgs(body.Id));

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
                link = link.Link(bodyLists);

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
                link = link.Link(bodyLists);

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
                link = link.Link(bodyLists);

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
                link = TopLeft.AabbQueryBroad(link, in aabb, selector);
                link = TopRight.AabbQueryBroad(link, in aabb, selector);

                link = BottomRight.AabbQueryBroad(link, in aabb, selector);
                link = BottomLeft.AabbQueryBroad(link, in aabb, selector);
            }
            else if (Count != 0)
            {
                link = link.Link(out var results);

                foreach (var bodyList in bodyLists)
                foreach (var bodyId in bodyList)
                {
                    if (!selector?.Invoke(bodyId) ?? false)
                        continue;

                    // All queries should be done using the current 
                    // bounding box, not the future one.
                    ref var body = ref Bodies.AtIndex(bodyId);

                    if (!Aabb.Intersects(body.BoundingBox, aabb))
                        continue;

                    results[(int)body.Type - 1].Add(bodyId);
                }

                if (!results.Any(l => l.Count != 0))
                    link = link.Unlink();
            }

            return link;
        }

        public QuadTreeNodeLink RayCastBroad(QuadTreeNodeLink link,
                                             in Vector2 a,
                                             in Vector2 b,
                                             BodySelector selector = null)
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

                foreach (var bodyList in bodyLists)
                foreach (var bodyId in bodyList)
                {
                    if (!selector?.Invoke(bodyId) ?? false)
                        continue;

                    // All queries should be done using the current 
                    // bounding box, not the future one.
                    ref var body = ref Bodies.AtIndex(bodyId);

                    if (!Line.Intersects(new Line(a, b), body.BoundingBox))
                        continue;

                    results[(int)body.Type - 1].Add(bodyId);
                }

                if (!results.Any(l => l.Count != 0))
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

    public delegate bool BodySelector(int bodyId);

    /// <summary>
    /// Class that represents a tree where each leaf has four children. 
    /// </summary>
    public sealed class QuadTree
    {
        #region Fields
        private readonly HashSet<int> active;
        private readonly BodyList     bodies;
        #endregion

        #region Events
        public event StructEventHandler<BodyEventArgs> Removed;

        public event StructEventHandler<BodyEventArgs> Added;
        #endregion

        #region Properties
        public Aabb BoundingBox
        {
            get;
        }

        public QuadTreeNode Root
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="QuadTree"/> using given configuration.
        /// </summary>
        /// <param name="nodeStaticBodyLimit">max static body limit for each node before split occurs</param>
        /// <param name="maxDepth">max depth in nodes</param>
        /// <param name="bounds">bounds of the tree</param>
        public QuadTree(BodyList bodies, int nodeStaticBodyLimit, int maxDepth, Vector2 bounds)
        {
            if (nodeStaticBodyLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeStaticBodyLimit));

            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));

            this.bodies = bodies ?? throw new ArgumentNullException(nameof(bodies));

            Root        = new QuadTreeNode(bodies, nodeStaticBodyLimit, maxDepth, bounds);
            BoundingBox = new Aabb(bounds * 0.5f, bounds * 0.5f);
            active      = new HashSet<int>();
        }

        /// <summary>
        /// Adds given body to the tree.
        /// </summary>
        public bool Add(int bodyId)
        {
            var added = Root.Add(bodies.WithId(bodyId));

            if (added)
                Added?.Invoke(this, new BodyEventArgs(bodyId));

            return added;
        }

        /// <summary>
        /// Removes given body from the tree.
        /// </summary>
        public bool Remove(int bodyId)
        {
            var removed = Root.Remove(bodies.WithId(bodyId));

            if (removed)
                Removed?.Invoke(this, new BodyEventArgs(bodyId));

            return removed;
        }

        /// <summary>
        /// Queries the whole tree to single link.
        /// </summary>
        public void RootQuery(QuadTreeNodeLink link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            Root.RootQuery(link);
        }

        /// <summary>
        /// Does narrow query to the tree using given AABB. Pass
        /// AABB as read only ref to ease memory pressure. Does not
        /// do AABB checks between bodies, checks only nodes.
        /// </summary>
        public void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            Root.AabbQueryNarrow(link, in aabb);
        }

        /// <summary>
        /// Does narrow ray cast to the tree using ray represented
        /// as two vectors. Does not do AABB checks between bodies, checks only nodes.
        /// </summary>
        public void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            Root.RayCastNarrow(link, in a, in b);
        }

        /// <summary>
        /// Does broad ray cast to the tree using ray represented 
        /// as two vectors. Pass points as read only ref
        /// to ease memory pressure. Does AABB checks for bodies.
        /// </summary>
        public void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, BodySelector selector = null)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            Root.RayCastBroad(link, in a, in b, selector);
        }

        /// <summary>
        /// Does broad query to the tree using given AABB. Does AABB checks for bodies.
        /// </summary>
        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            Root.AabbQueryBroad(link, in aabb, selector);
        }

        /// <summary>
        /// Cleans the tree and returns all lost bodies. Bodies 
        /// that have their transformation updated during this
        /// frame will be checked. Bodies that are lost have moved
        /// away from at least of their nodes.
        /// </summary>
        public IEnumerable<int> RelocateMovingBodies()
        {
            Root.GetActiveBodies(active);

            if (active.Count == 0)
                return Enumerable.Empty<int>();

            IEnumerable<int> GetEnumeration()
            {
                foreach (var bodyId in active)
                {
                    Remove(bodyId);

                    if (!Add(bodyId))
                        yield return bodyId;
                }

                active.Clear();
            }

            return GetEnumeration();
        }
    }
}