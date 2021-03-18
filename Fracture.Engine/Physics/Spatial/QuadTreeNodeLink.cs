using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Engine.Physics.Dynamics;

namespace Fracture.Engine.Physics.Spatial
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
        private readonly CollectionPool<List<int>, int> listPool;
        private readonly DelegatePool<QuadTreeNodeLink> linkPool;
        private readonly ArrayPool<List<int>> arrayPool;

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

        public IEnumerable<int> Sensors
            => bodyLists[(int)BodyType.Sensor - 1];

        public IEnumerable<int> Statics
            => bodyLists[(int)BodyType.Static - 1];

        public IEnumerable<int> Dynamics
            => bodyLists[(int)BodyType.Dynamic - 1];

        public IEnumerable<int> Bodies
            => Sensors.Concat(Statics).Concat(Dynamics);

        /// <summary>
        /// Returns boolean declaring whether this is the 
        /// last node in this link.
        /// </summary>
        public bool End
            => Next == null || bodyLists == null;
        #endregion

        public QuadTreeNodeLink()
        {
            linkPool = new DelegatePool<QuadTreeNodeLink>(
                new LinearStorageObject<QuadTreeNodeLink>(
                    new LinearGrowthArray<QuadTreeNodeLink>()),
                Clone);

            listPool = new CollectionPool<List<int>, int>(
                new DelegatePool<List<int>>(
                    new LinearStorageObject<List<int>>(
                        new LinearGrowthArray<List<int>>()), () => new List<int>()));

            arrayPool = new ArrayPool<List<int>>(
                () => new LinearStorageObject<List<int>[]>(new LinearGrowthArray<List<int>[]>()), 8);
        }

        private QuadTreeNodeLink(DelegatePool<QuadTreeNodeLink> linkPool, 
                                 CollectionPool<List<int>, int> listPool,
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
        public QuadTreeNodeLink Link(out List<int>[] bodies)
        {
            owner     = true;
            bodies = arrayPool.Take(ListsCount);

            for (var i = 0; i < bodies.Length; i++)
                bodies[i] = listPool.Take();

            // Link.
            return Link(bodies);
        }

        /// <summary>
        /// Unlinks current node and links to previous.
        /// </summary>
        public QuadTreeNodeLink Unlink()
        {
            if (previous == null) return this;

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
                listPool.Return(bodyLists);
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
}
