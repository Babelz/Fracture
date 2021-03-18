using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Spatial
{
    /// <summary>
    /// Class that represents a tree where each leaf has four children. 
    /// </summary>
    public sealed class QuadTree 
    {
        #region Constant fields
        private const int InitialBodyBufferCapacity = 512;
        #endregion
        
        #region Fields
        private readonly HashSet<int> active;
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
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="QuadTree"/> using given configuration.
        /// </summary>
        /// <param name="bodies">body list this tree is presenting</param>
        /// <param name="nodeBodyLimit">max body limit for each node before split occurs</param>
        /// <param name="maxDepth">max depth in nodes</param>
        /// <param name="bounds">bounds of the tree</param>
        public QuadTree(BodyList bodies, int nodeBodyLimit, int maxDepth, Vector2 bounds)
        {
            if (nodeBodyLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(nodeBodyLimit));

            if (maxDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxDepth));
            
            Root        = new QuadTreeNode(bodies, nodeBodyLimit, maxDepth, bounds);
            BoundingBox = new Aabb(bounds * 0.5f, bounds * 0.5f);
            active      = new HashSet<int>(InitialBodyBufferCapacity);
        }

        /// <summary>
        /// Adds given body to the tree.
        /// </summary>
        public bool Add(int id)
        {
            var added = Root.Add(id);

            if (added)
                Added?.Invoke(this, new BodyEventArgs(id));
            
            return added;
        }

        /// <summary>
        /// Removes given body from the tree.
        /// </summary>
        public bool Remove(int id)   
        {
            var removed = Root.Remove(id);
            
            if (removed)
                Removed?.Invoke(this, new BodyEventArgs(id));
        
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

            Root.AabbQueryNarrow(link, aabb);
        }

        /// <summary>
        /// Does narrow ray cast to the tree using ray represented
        /// as two vectors. Does not do AABB checks between bodies, checks only nodes.
        /// </summary>
        public void RayCastNarrow(QuadTreeNodeLink link, in Line line)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.RayCastNarrow(link, line);
        }

        /// <summary>
        /// Does broad ray cast to the tree using ray represented 
        /// as two vectors. Pass points as read only ref
        /// to ease memory pressure. Does AABB checks for bodies.
        /// </summary>
        public void RayCastBroad(QuadTreeNodeLink link, in Line line, BodySelector selector = null)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.RayCastBroad(link, line, selector);
        }

        /// <summary>
        /// Does broad query to the tree using given AABB. Does AABB checks for bodies.
        /// </summary>
        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null)
        {
            if (link == null) throw new ArgumentNullException(nameof(link));

            Root.AabbQueryBroad(link, aabb, selector);
        }

        /// <summary>
        /// Cleans the tree and returns all lost bodies. Bodies 
        /// that have their transformation updated during this
        /// frame will be checked. Bodies that are lost have moved
        /// away from at least of their nodes.
        /// </summary>
        public IEnumerable<int> RelocateLostBodies()
        {
            Root.GetActiveBodies(active);

            if (active.Count == 0)
                return Enumerable.Empty<int>();

            IEnumerable<int> GetEnumeration()
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
