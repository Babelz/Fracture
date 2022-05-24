using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Contacts;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;
using NLog;

namespace Fracture.Engine.Physics
{
    public interface IPhysicsWorldSystem : IGameEngineSystem
    {
        #region Events
        event EventHandler<BodyContactEventArgs> BeginContact;
        event EventHandler<BodyContactEventArgs> EndContact;
        
        event EventHandler<BodyEventArgs> Removed;
        event EventHandler<BodyEventArgs> Added;
        
        event EventHandler<BodyEventArgs> Moving;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the quad tree of the world. Use the tree at your 
        /// own risk.
        /// </summary>
        QuadTree Tree
        {
            get;
        }
        #endregion

        Body Create(BodyType type, Shape shape, Vector2 position, float angle);
        Body Create(BodyType type, Shape shape, Vector2 position);
        Body Create(BodyType type, Shape shape);
        
        void Delete(Body body);
        
        void RootQuery(QuadTreeNodeLink link);
        QuadTreeNodeLink RootQuery();
        
        void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, Func<Body, bool> selector = null);
        QuadTreeNodeLink RayCastBroad(in Vector2 a, in Vector2 b, Func<Body, bool> selector = null);
        
        void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b);
        QuadTreeNodeLink RayCastNarrow(in Vector2 a, in Vector2 b);
        
        void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, Func<Body, bool> selector = null);
        QuadTreeNodeLink AabbQueryBroad(in Aabb aabb, Func<Body, bool> selector = null);
        
        void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb);
        QuadTreeNodeLink AabbQueryNarrow(in Aabb aabb);
        
        IEnumerable<Body> ContactsOf(Body body);
    }

    /// <summary>
    /// World that contains the physics simulation for handling collisions between bodies.
    /// </summary>
    public sealed class PhysicsWorldSystem : GameEngineSystem, IPhysicsWorldSystem
    {
        #region Fields
        private readonly BodyContactEventArgs beginArgs;
        private readonly BodyContactEventArgs endArgs;

        private readonly Dictionary<Body, ContactList> contactListLookup;
        private readonly List<ContactList> contactLists;

        private readonly Pool<Body> bodyPool;

        private readonly QuadTreeNodeLink rootLink;
        private readonly QuadTreeNodeLink rayCastBroadLink;
        private readonly QuadTreeNodeLink rayCastNarrowLink;
        private readonly QuadTreeNodeLink aabbQueryBroadLink;
        private readonly QuadTreeNodeLink aabbQueryNarrowLink;

        private readonly NarrowPhaseContactSolver narrow;
        private readonly BroadPhaseContactSolver broad;

        private readonly List<Body> lost;
        private readonly List<Body> moving;

        private ulong frame;
        #endregion
        
        #region Events
        public event EventHandler<BodyContactEventArgs> BeginContact;
        public event EventHandler<BodyContactEventArgs> EndContact;

        public event EventHandler<BodyEventArgs> Removed;
        public event EventHandler<BodyEventArgs> Added;
        
        public event EventHandler<BodyEventArgs> Moving;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the quad tree of the world. Use the tree at your 
        /// own risk.
        /// </summary>
        public QuadTree Tree
        {
            get;
        }
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="PhysicsWorldSystem"/> with given configuration.
        /// </summary>
        /// <param name="treeNodeStaticBodyLimit">max static body limit for each node before split occurs</param>
        /// <param name="treeNodeMaxDepth">max depth in nodes</param>
        /// <param name="bounds">bounds of the tree</param>
        [BindingConstructor]
        public PhysicsWorldSystem(int treeNodeStaticBodyLimit, int treeNodeMaxDepth, float bounds)
        {
            beginArgs = new BodyContactEventArgs();
            endArgs   = new BodyContactEventArgs();

            rootLink            = new QuadTreeNodeLink();
            rayCastBroadLink    = new QuadTreeNodeLink();
            rayCastNarrowLink   = new QuadTreeNodeLink();
            aabbQueryBroadLink  = new QuadTreeNodeLink();
            aabbQueryNarrowLink = new QuadTreeNodeLink();

            bodyPool = new Pool<Body>(
                new LinearStorageObject<Body>(
                    new LinearGrowthArray<Body>(
                        64, 1)), 64);

            contactListLookup = new Dictionary<Body, ContactList>();
            contactLists      = new List<ContactList>();

            Tree   = new QuadTree(treeNodeStaticBodyLimit, treeNodeMaxDepth, new Vector2(bounds));
            narrow = new NarrowPhaseContactSolver();
            broad  = new BroadPhaseContactSolver();
            lost   = new List<Body>();
            moving = new List<Body>();
            
            Tree.Added   += Tree_Added;
            Tree.Removed += Tree_Removed;
        }

        #region Event handlers
        private void Tree_Removed(object sender, BodyEventArgs e)
            => Added?.Invoke(this, e);

        private void Tree_Added(object sender, BodyEventArgs e)
            => Added?.Invoke(this, e);
        #endregion

        private void AddLostBody(Body body)
        {
            // Compute distance between bounding box points.
            var bb = body.TransformBoundingBox;
            var tb = Tree.BoundingBox;

            // Delta top.
            var dt = bb.Top - tb.Top;

            // Right.
            var dr = tb.Right - bb.Right;
            
            // Bottom.
            var db = tb.Bottom - bb.Bottom;

            // Left.
            var dl = tb.Left - bb.Left;

            // Compute transform.
            var tr = bb.Position;

            // Overlap left or right.
            if      (dr < 0.0f) tr.X += dr;
            else if (dl < 0.0f) tr.X += dl;

            // Overlap top or bottom.
            if      (dt < 0.0f) tr.Y += dt;
            else if (db < 0.0f) tr.Y += dt;

            // Apply overlap translation and update.
            body.Transform(tr, body.Angle);

            body.ApplyTransformation();

            // Should never happen.
            if (!Tree.Add(body))
                lost.Add(body);
        }

        private void ApplyTransformations(QuadTreeNode node)
        {
            if (node.IsSplit)
            {
                ApplyTransformations(node.TopLeft);
                ApplyTransformations(node.TopRight);
                ApplyTransformations(node.BottomRight);
                ApplyTransformations(node.BottomLeft);

                return;
            }

            foreach (var body in node.Dynamics)
            {
                if (body.Active)
                {
                    body.ApplyTransformation();

                    moving.Add(body);
                }
            }

            foreach (var body in node.Sensors)
            {
                if (body.Active)
                {
                    body.ApplyTransformation();

                    moving.Add(body);
                }
            }
        }

        private void NormalizeTransformations(QuadTreeNode node, float delta)
        {
            if (node.IsSplit)
            {
                NormalizeTransformations(node.TopLeft, delta);
                NormalizeTransformations(node.TopRight, delta);
                NormalizeTransformations(node.BottomRight, delta);
                NormalizeTransformations(node.BottomLeft, delta);

                return;
            }

            foreach (var body in node.Dynamics)
            {
                if (body.Active)
                {
                    if (body.Translating && !body.Normalized)
                        body.NormalizeTransformation(delta);
                }
            }

            foreach (var body in node.Sensors)
            {
                if (body.Active)
                {
                    if (body.Translating && !body.Normalized)
                        body.NormalizeTransformation(delta);
                }
            }
        }

        public Body Create(BodyType type, Shape shape, Vector2 position, float angle)
        {
            var body = bodyPool.Take();

            body.Setup(type, shape, position, angle);

            if (!Tree.Add(body))
                AddLostBody(body);

            if (type != BodyType.Static)
            {
                // Static bodies should not have contact lists.
                var contactList = new ContactList(body);

                contactLists.Add(contactList);
                contactListLookup.Add(body, contactList);
            }

            return body;
        }

        public Body Create(BodyType type, Shape shape, Vector2 position)
            => Create(type, shape, position, 0.0f);

        public Body Create(BodyType type, Shape shape)
            => Create(type, shape, Vector2.Zero, 0.0f);
        
        public void Delete(Body body)
        {
            if (!Tree.Remove(body))
                throw new InvalidOperationException("could not delete body");

            if (body.Type != BodyType.Static)
            {
                contactLists.Remove(contactListLookup[body]);
                contactListLookup.Remove(body);
            }
            
            Removed?.Invoke(this, new BodyEventArgs(body));

            bodyPool.Return(body);
        }

        public void RootQuery(QuadTreeNodeLink link)
        {
            link.Clear();

            Tree.RootQuery(link);
        }

        public void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, Func<Body, bool> selector = null)
        {
            link.Clear();

            Tree.RayCastBroad(link, in a, in b, selector);
        }

        public void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b)
        {
            link.Clear();

            Tree.RayCastNarrow(link, in a, in b);
        }

        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, Func<Body, bool> selector = null)
        {
            link.Clear();

            Tree.AabbQueryBroad(link, aabb, selector);
        }

        public void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb)
        {
            link.Clear();

            Tree.AabbQueryNarrow(link, aabb);
        }

        public QuadTreeNodeLink RootQuery()
        {
            RootQuery(rootLink);

            return rootLink;
        }

        public QuadTreeNodeLink RayCastBroad(in Vector2 a, in Vector2 b, Func<Body, bool> selector = null)
        {
            RayCastBroad(rayCastBroadLink, in a, in b, selector);

            return rayCastBroadLink;
        }

        public QuadTreeNodeLink RayCastNarrow(in Vector2 a, in Vector2 b)
        {
            RayCastNarrow(rayCastNarrowLink, in a, in b);

            return rayCastNarrowLink;
        }

        public QuadTreeNodeLink AabbQueryBroad(in Aabb aabb, Func<Body, bool> selector = null)
        {
            AabbQueryBroad(aabbQueryBroadLink, in aabb, selector);

            return aabbQueryBroadLink;
        }

        public QuadTreeNodeLink AabbQueryNarrow(in Aabb aabb)
        {
            AabbQueryNarrow(aabbQueryNarrowLink, in aabb);

            return aabbQueryNarrowLink;
        }

        public IEnumerable<Body> ContactsOf(Body body)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));

            if (contactListLookup.TryGetValue(body, out var contactList))
                return contactList.CurrentContacts;

            return Enumerable.Empty<Body>();
        }

        public void Update(IGameEngineTime time)
        {
            // Steps for running the so called simulation are as follows:
            // 
            // 1) go trough all dynamics and sensors and normalize translations 
            // 2) sweep the quad tree, reposition "lost" bodies
            // 3) do broad phase pairing for bodies
            // 4) do narrow phase checks for bodies
            // 5) update contact lists 
            // 6) apply MTV translations
            // 7) invoke contact events

            // Advance frame to keep contact lists in check.
            frame++;

            // Normalize translations. Determine delta.
            var delta = (float)time.Elapsed.TotalSeconds;
            var node  = Tree.Root;

            // Normalize user transformations before solve.
            NormalizeTransformations(node, delta);

            // Solve all broad pairs.
            broad.Solve(Tree, delta);

            // Apply user transformations after solve.
            ApplyTransformations(node);

            // Sweep quad tree and re-position lost bodies.
            foreach (var lost in Tree.RelocateMovingBodies())
                AddLostBody(lost);

            // Solve all narrow pairs.
            while (broad.Count != 0)
            {
                // Narrow solve pair.
                ref var pair = ref broad.Next();
                
                narrow.Solve(pair.A, pair.B);

                // Handle all narrow pairs.
                while (narrow.Count != 0)
                {
                    ref var contact = ref narrow.Next();

                    // Apply MTV translation.
                    if (contact.A.Type == BodyType.Dynamic)
                    {
                        contact.A.Translate(contact.Translation);

                        contact.A.ApplyTransformation();
                    }

                    // Update contact lists.
                    contactListLookup[contact.A].Add(contact.B, frame);
                }
            }
            
            // Invoke all contact events.
            for (var i = 0; i < contactLists.Count; i++)
            {
                // Invoke all begin contact events.
                beginArgs.Body = contactLists[i].Body;

                foreach (var enteringBody in contactLists[i].EnteringContacts)
                {
                    beginArgs.Contact = enteringBody;

                    BeginContact?.Invoke(this, beginArgs);
                }

                // Invoke all end contact events.
                endArgs.Body = contactLists[i].Body;

                foreach (var leavingBody in contactLists[i].LeavingContacts)
                {
                    endArgs.Contact = leavingBody;

                    EndContact?.Invoke(this, endArgs);
                }
            }
            
            foreach (var body in moving)
            {
                Moving?.Invoke(this, new BodyEventArgs(body));
            }
            
            moving.Clear();
            
            for (var i = 0; i < lost.Count; i++)
            {
                if (Tree.Add(lost[i]))
                    lost.RemoveAt(i);
            }
        }
    }
}
