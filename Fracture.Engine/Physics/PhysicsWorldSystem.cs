using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Events;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Contacts;
using Fracture.Engine.Physics.Dynamics;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics
{
    public readonly struct BodyContactEventArgs : IStructEventArgs
    {
        #region Properties
        public int BodyId
        {
            get;
        }

        public int ContactId
        {
            get;
        }
        #endregion

        public BodyContactEventArgs(int bodyId, int contactId)
        {
            BodyId    = bodyId;
            ContactId = contactId;
        }
    }

    public interface IPhysicsWorldSystem : IGameEngineSystem
    {
        #region Events
        event StructEventHandler<BodyContactEventArgs> BeginContact;

        event StructEventHandler<BodyContactEventArgs> EndContact;

        event StructEventHandler<BodyEventArgs> Removed;

        event StructEventHandler<BodyEventArgs> Added;

        event StructEventHandler<BodyEventArgs> Moving;
        #endregion

        #region Properties
        QuadTree Tree
        {
            get;
        }

        BodyList Bodies
        {
            get;
        }
        #endregion

        int Create(BodyType type, in IShape shape, in Vector2 position, float rotation, object userData = null);
        int Create(BodyType type, in IShape shape, in Vector2 position, object userData = null);
        int Create(BodyType type, in IShape shape, object userData = null);

        void Delete(int bodyId);

        void RootQuery(QuadTreeNodeLink link);
        QuadTreeNodeLink RootQuery();

        void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, BodySelector selector = null);
        QuadTreeNodeLink RayCastBroad(in Vector2 a, in Vector2 b, BodySelector selector = null);

        void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b);
        QuadTreeNodeLink RayCastNarrow(in Vector2 a, in Vector2 b);

        void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null);
        QuadTreeNodeLink AabbQueryBroad(in Aabb aabb, BodySelector selector = null);

        void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb);
        QuadTreeNodeLink AabbQueryNarrow(in Aabb aabb);

        IEnumerable<int> ContactsOf(int bodyId);
    }

    /// <summary>
    /// World that contains the physics simulation for handling collisions between bodies.
    /// </summary>
    public sealed class PhysicsWorldSystem : GameEngineSystem, IPhysicsWorldSystem
    {
        #region Fields
        private readonly Dictionary<int, ContactList> contactListLookup;
        private readonly List<ContactList>            contactLists;

        private readonly QuadTreeNodeLink rootLink;
        private readonly QuadTreeNodeLink rayCastBroadLink;
        private readonly QuadTreeNodeLink rayCastNarrowLink;
        private readonly QuadTreeNodeLink aabbQueryBroadLink;
        private readonly QuadTreeNodeLink aabbQueryNarrowLink;

        private readonly NarrowPhaseContactSolver narrow;
        private readonly BroadPhaseContactSolver  broad;

        private readonly HashSet<int> lost;
        private readonly HashSet<int> moving;

        private ulong frame;
        #endregion

        #region Events
        public event StructEventHandler<BodyContactEventArgs> BeginContact;

        public event StructEventHandler<BodyContactEventArgs> EndContact;

        public event StructEventHandler<BodyEventArgs> Removed;

        public event StructEventHandler<BodyEventArgs> Added;

        public event StructEventHandler<BodyEventArgs> Moving;
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

        /// <summary>
        /// Returns the body list of this world. Modify the bodies at
        /// your own risk.
        /// </summary>
        public BodyList Bodies
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
            rootLink            = new QuadTreeNodeLink();
            rayCastBroadLink    = new QuadTreeNodeLink();
            rayCastNarrowLink   = new QuadTreeNodeLink();
            aabbQueryBroadLink  = new QuadTreeNodeLink();
            aabbQueryNarrowLink = new QuadTreeNodeLink();

            contactListLookup = new Dictionary<int, ContactList>();
            contactLists      = new List<ContactList>();
            lost              = new HashSet<int>();
            moving            = new HashSet<int>();
            Bodies            = new BodyList();

            Tree   = new QuadTree(Bodies, treeNodeStaticBodyLimit, treeNodeMaxDepth, new Vector2(bounds));
            narrow = new NarrowPhaseContactSolver();
            broad  = new BroadPhaseContactSolver();

            Tree.Added   += Tree_Added;
            Tree.Removed += Tree_Removed;
        }

        #region Event handlers
        private void Tree_Removed(object sender, in BodyEventArgs args)
            => Added?.Invoke(this, args);

        private void Tree_Added(object sender, in BodyEventArgs args)
            => Added?.Invoke(this, args);
        #endregion

        private void AddLostBody(ref Body body)
        {
            // Compute distance between bounding box points.
            var bb = body.TransformBoundingBox;
            var tb = Tree.BoundingBox;
            var tr = body.Position;

            // Overlap left or right.
            if (bb.Right > tb.Right)
                tr.X = tb.Right - bb.HalfBounds.X;
            else if (bb.Left < tb.Left)
                tr.X = tb.Left + bb.HalfBounds.X;

            // Overlap top or bottom.
            if (bb.Bottom > tb.Bottom)
                tr.Y = tb.Bottom - bb.HalfBounds.Y;
            else if (bb.Top < tb.Top)
                tr.Y = tb.Top + bb.HalfBounds.Y;

            // Apply overlap translation and update.
            body.Transform(tr, body.Rotation);

            body.ApplyTransformation();

            // Should never happen.
            if (!Tree.Add(body.Id))
                lost.Add(body.Id);
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

            foreach (var bodyId in node.Dynamics)
            {
                ref var body = ref Bodies.AtIndex(bodyId);

                if (!body.Active)
                    continue;

                body.ApplyTransformation();

                moving.Add(bodyId);
            }

            foreach (var bodyId in node.Sensors)
            {
                ref var body = ref Bodies.AtIndex(bodyId);

                if (!body.Active)
                    continue;

                body.ApplyTransformation();

                moving.Add(bodyId);
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

            foreach (var bodyId in node.Dynamics)
            {
                ref var body = ref Bodies.AtIndex(bodyId);

                if (!body.Active)
                    continue;

                if (body.Translating && !body.Normalized)
                    body.NormalizeTransformation(delta);
            }

            foreach (var bodyId in node.Sensors)
            {
                ref var body = ref Bodies.AtIndex(bodyId);

                if (!body.Active)
                    continue;

                if (body.Translating && !body.Normalized)
                    body.NormalizeTransformation(delta);
            }
        }

        public int Create(BodyType type, in IShape shape, in Vector2 position, float rotation, object userData = null)
        {
            var bodyId = Bodies.Create(type, shape, position, rotation, userData);

            if (!Tree.Add(bodyId))
                AddLostBody(ref Bodies.AtIndex(bodyId));

            if (type == BodyType.Static)
                return bodyId;

            // Static bodies should not have contact lists.
            var contactList = new ContactList(bodyId);

            contactLists.Add(contactList);
            contactListLookup.Add(bodyId, contactList);

            return bodyId;
        }

        public int Create(BodyType type, in IShape shape, in Vector2 position, object userData = null)
            => Create(type, shape, position, 0.0f, userData);

        public int Create(BodyType type, in IShape shape, object userData = null)
            => Create(type, shape, Vector2.Zero, 0.0f, userData);

        public void Delete(int bodyId)
        {
            if (!Tree.Remove(bodyId))
                throw new InvalidOperationException("could not delete body");

            ref var body = ref Bodies.AtIndex(bodyId);

            if (body.Type != BodyType.Static)
            {
                contactLists.Remove(contactListLookup[bodyId]);
                contactListLookup.Remove(bodyId);
            }

            Removed?.Invoke(this, new BodyEventArgs(bodyId));
        }

        public void RootQuery(QuadTreeNodeLink link)
        {
            link.Clear();

            Tree.RootQuery(link);
        }

        public void RayCastBroad(QuadTreeNodeLink link, in Vector2 a, in Vector2 b, BodySelector selector = null)
        {
            link.Clear();

            Tree.RayCastBroad(link, in a, in b, selector);
        }

        public void RayCastNarrow(QuadTreeNodeLink link, in Vector2 a, in Vector2 b)
        {
            link.Clear();

            Tree.RayCastNarrow(link, in a, in b);
        }

        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null)
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

        public QuadTreeNodeLink RayCastBroad(in Vector2 a, in Vector2 b, BodySelector selector = null)
        {
            RayCastBroad(rayCastBroadLink, in a, in b, selector);

            return rayCastBroadLink;
        }

        public QuadTreeNodeLink RayCastNarrow(in Vector2 a, in Vector2 b)
        {
            RayCastNarrow(rayCastNarrowLink, in a, in b);

            return rayCastNarrowLink;
        }

        public QuadTreeNodeLink AabbQueryBroad(in Aabb aabb, BodySelector selector = null)
        {
            AabbQueryBroad(aabbQueryBroadLink, in aabb, selector);

            return aabbQueryBroadLink;
        }

        public QuadTreeNodeLink AabbQueryNarrow(in Aabb aabb)
        {
            AabbQueryNarrow(aabbQueryNarrowLink, in aabb);

            return aabbQueryNarrowLink;
        }

        public IEnumerable<int> ContactsOf(int bodyId)
        {
            if (contactListLookup.TryGetValue(bodyId, out var contactList))
                return contactList.CurrentContacts;

            return Enumerable.Empty<int>();
        }

        public override void Update(IGameEngineTime time)
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

            foreach (var contactList in contactLists)
                contactList.Update();

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
            foreach (var lostBodyId in Tree.RelocateMovingBodies())
                AddLostBody(ref Bodies.AtIndex(lostBodyId));

            // Solve all narrow pairs.
            while (broad.ContainsPairs)
            {
                // Narrow solve pair.
                ref var pair = ref broad.Next();

                narrow.Solve(Bodies.AtIndex(pair.FirstBodyId), Bodies.AtIndex(pair.SecondBodyId));

                // Handle all narrow pairs.
                while (narrow.ContainsContacts)
                {
                    ref var contact = ref narrow.Next();

                    ref var a = ref Bodies.AtIndex(contact.FirstBodyId);
                    ref var b = ref Bodies.AtIndex(contact.SecondBodyId);

                    // Apply MTV translation.
                    if (a.Type == BodyType.Dynamic)
                    {
                        a.Translate(contact.Translation);

                        a.ApplyTransformation();
                    }

                    // Update contact lists.
                    contactListLookup[a.Id].Add(b.Id);
                }
            }

            // Invoke all contact events.
            for (var i = 0; i < contactLists.Count; i++)
            {
                // Invoke all begin contact events.
                foreach (var enteringBody in contactLists[i].EnteringContacts)
                    BeginContact?.Invoke(this, new BodyContactEventArgs(contactLists[i].BodyId, enteringBody));

                // Invoke all end contact events.
                foreach (var leavingBody in contactLists[i].LeavingContacts)
                    EndContact?.Invoke(this, new BodyContactEventArgs(contactLists[i].BodyId, leavingBody));
            }

            foreach (var movingBodyId in moving)
                Moving?.Invoke(this, new BodyEventArgs(movingBodyId));

            moving.Clear();
        }
    }
}