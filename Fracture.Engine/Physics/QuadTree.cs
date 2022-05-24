using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics
{
    /// <summary>
    /// Class that represents a tree where each leaf has four children. 
    /// </summary>
    public sealed class QuadTree 
    {
        #region Fields
        private readonly HashSet<Body> active;
        #endregion

        #region Events
        public event EventHandler<BodyEventArgs> Removed;
        public event EventHandler<BodyEventArgs> Added;
        #endregion

        #region Properties
        public Aabb BoundingBox
        {
            get;
        }
        
        public QuadTreeNode Root
        {
            get;
            private set;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="QuadTree"/> using given configuration.
        /// </summary>
        /// <param name="nodeStaticBodyLimit">max static body limit for each node before split occurs</param>
        /// <param name="maxDepth">max depth in nodes</param>
        /// <param name="bounds">bounds of the tree</param>
        public QuadTree(int nodeStaticBodyLimit, int maxDepth, Vector2 bounds)
        {
            if (nodeStaticBodyLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeStaticBodyLimit));

            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));

            Root        = new QuadTreeNode(nodeStaticBodyLimit, maxDepth, bounds);
            BoundingBox = new Aabb(bounds * 0.5f, bounds * 0.5f);
            active        = new HashSet<Body>();
        }

        /// <summary>
        /// Adds given body to the tree.
        /// </summary>
        public bool Add(Body body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var added = Root.Add(body);

            if (added)
                Added?.Invoke(this, new BodyEventArgs(body));
            
            return added;
        }

        /// <summary>
        /// Removes given body from the tree.
        /// </summary>
        public bool Remove(Body body)   
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var removed = Root.Remove(body);
            
            if (removed)
                Removed?.Invoke(this, new BodyEventArgs(body));
        
            return removed;
        }

        /// <summary>
        /// Queries the whole tree to single link.
        /// </summary>
        public void RootQuery(QuadTreeNodeLink link)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.RootQuery(link);
        }

        /// <summary>
        /// Does narrow query to the tree using given AABB. Pass
        /// AABB as read only ref to ease memory pressure. Does not
        /// do AABB checks between bodies, checks only nodes.
        /// </summary>
        public void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.AabbQueryNarrow(link, in aabb);
        }

        /// <summary>
        /// Does narrow ray cast to the tree using ray represented
        /// as two vectors. Does not do AABB checks between bodies, checks only nodes.
        /// </summary>
        public void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.RayCastNarrow(link, in a, in b);
        }

        /// <summary>
        /// Does broad ray cast to the tree using ray represented 
        /// as two vectors. Pass points as read only ref
        /// to ease memory pressure. Does AABB checks for bodies.
        /// </summary>
        public void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, Func<Body, bool> selector = null)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.RayCastBroad(link, in a, in b, selector);
        }

        /// <summary>
        /// Does broad query to the tree using given AABB. Does AABB checks for bodies.
        /// </summary>
        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, Func<Body, bool> selector = null)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.AabbQueryBroad(link, in aabb, selector);
        }

        /// <summary>
        /// Cleans the tree and returns all lost bodies. Bodies 
        /// that have their transformation updated during this
        /// frame will be checked. Bodies that are lost have moved
        /// away from at least of their nodes.
        /// </summary>
        public IEnumerable<Body> RelocateMovingBodies()
        {
            Root.GetActiveBodies(active);

            if (active.Count == 0)
                return Enumerable.Empty<Body>();

            IEnumerable<Body> GetEnumeration()
            {
                foreach (var body in active)
                {
                    Remove(body);

                    if (!Add(body))
                        yield return body;
                }

                active.Clear();
            }

            return GetEnumeration();
        }
    }
}
