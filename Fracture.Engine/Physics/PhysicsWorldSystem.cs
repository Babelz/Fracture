using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Physics.Contacts;
using Fracture.Engine.Physics.Dynamics;
using Fracture.Engine.Physics.Spatial;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics
{
    /// <summary>
    /// Event arguments that contains single contact event arguments. Use structure to ease
    /// memory pressure when creating the events.
    /// </summary>
    public readonly struct BodyContactEventArgs
    {
        #region Properties
        public int FirstBodyId
        {
            get;
        }

        public int SecondBodyId
        {
            get;
        }

        public Vector2 Translation
        {
            get;
        }
        #endregion

        public BodyContactEventArgs(int firstBodyId, int secondBodyId, Vector2 translation)
        {
            FirstBodyId  = firstBodyId;
            SecondBodyId = secondBodyId;
            Translation  = translation;
        }
    }

    public interface IPhysicsWorldSystem : IObjectManagementSystem
    {
        #region Events
        /// <summary>
        /// Event invoked when body moved.
        /// </summary>
        event EventHandler<BodyEventArgs> Moved;

        /// <summary>
        /// Event invoked when two bodies begin contacting.
        /// </summary>
        event EventHandler<BodyContactEventArgs> BeginContact;

        /// <summary>
        /// Event invoked when two bodies stop contacting.
        /// </summary>
        event EventHandler<BodyContactEventArgs> EndContact;

        /// <summary>
        /// Event invoked when body is removed from the world.
        /// </summary>
        event EventHandler<BodyEventArgs> Removed;

        /// <summary>
        /// Event invoked when body is added to the world.
        /// </summary>
        event EventHandler<BodyEventArgs> Added;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the gravity force of the world. This value is interpreted as meters per second (m/s).
        /// </summary>
        float Gravity
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the wind force of the world. This value is interpreted as meters per second (m/s).
        /// </summary>
        float Wind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the fixed simulation time step timer of the world.
        /// </summary>
        TimeSpan FixedTimeStep
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the body list containing all bodies in the world. Mutate
        /// values of these bodies at your own risk.
        /// </summary>
        IReadOnlyBodyList Bodies
        {
            get;
        }
        #endregion

        int Create(BodyType type,
                   in Vector2 position,
                   float rotation,
                   in Shape shape,
                   object userData = null);

        int Create(BodyType type, in Vector2 position, in Shape shape, object userData = null);
        int Create(BodyType type, in Shape shape, object userData = null);

        void Delete(int bodyId);

        /// <summary>
        /// Resizes the simulation area of the world. This is heavy operation as the whole
        /// quad tree must be rebuild. Calling this each frame in tight loops will kill your
        /// performance fast.
        /// </summary>
        void Resize(Vector2 bounds);

        void RootQuery(QuadTreeNodeLink link);

        void RayCastBroad(QuadTreeNodeLink link, in Line line, BodySelector selector = null);
        void RayCastNarrow(QuadTreeNodeLink link, in Line line);

        void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null);
        void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb);

        QuadTreeNodeLink RootQuery();

        QuadTreeNodeLink RayCastBroad(in Line line, BodySelector selector = null);
        QuadTreeNodeLink RayCastNarrow(in Line line);
        QuadTreeNodeLink AabbQueryBroad(in Aabb aabb, BodySelector selector = null);
        QuadTreeNodeLink AabbQueryNarrow(in Aabb aabb);

        IEnumerable<int> ContactsOf(int bodyId);
    }

    /// <summary>
    /// Class that provides physics simulation and collision detection for the game engine.
    ///
    /// TODO: does not support large speeds, add integration logic for handling that.
    /// </summary>
    public sealed class PhysicsWorldSystem : GameEngineSystem, IPhysicsWorldSystem
    {
        #region Static fields
        /// <summary>
        /// Default delta adjusted for 60 fps operations.
        /// </summary>
        public static readonly TimeSpan DefaultDelta = TimeSpan.FromMilliseconds(16.6666);
        #endregion

        #region Fields
        private readonly Dictionary<int, ContactList> contactListLookup;
        private readonly List<ContactList> contactLists;

        private readonly QuadTreeNodeLink rootLink;
        private readonly QuadTreeNodeLink rayCastBroadLink;
        private readonly QuadTreeNodeLink rayCastNarrowLink;
        private readonly QuadTreeNodeLink aabbQueryBroadLink;
        private readonly QuadTreeNodeLink aabbQueryNarrowLink;

        private readonly BroadPhaseContactSolver broad;
        private readonly BodyList bodies;

        private QuadTree tree;

        private readonly int treeNodeBodyLimit;
        private readonly int treeNodeMaxDepth;

        private ulong frame;
        #endregion

        #region Events
        public event EventHandler<BodyEventArgs> Moved;

        public event EventHandler<BodyContactEventArgs> BeginContact;
        public event EventHandler<BodyContactEventArgs> EndContact;

        public event EventHandler<BodyEventArgs> Removed;
        public event EventHandler<BodyEventArgs> Added;
        #endregion

        #region Properties
        public float Gravity
        {
            get;
            set;
        }

        public float Wind
        {
            get;
            set;
        }

        public TimeSpan FixedTimeStep
        {
            get;
            set;
        }

        public IReadOnlyBodyList Bodies => bodies;
        #endregion

        /// <summary>
        /// Creates new instance of <see cref="PhysicsWorldSystem"/> with given configuration.
        /// <param name="treeNodeBodyLimit">max bodies one node can hold in the tree before it will be split</param>
        /// <param name="treeNodeMaxDepth">max splits one node can have</param>
        /// </summary>
        [BindingConstructor]
        public PhysicsWorldSystem(int treeNodeBodyLimit = 16, int treeNodeMaxDepth = 8)
        {
            this.treeNodeBodyLimit = treeNodeBodyLimit;
            this.treeNodeMaxDepth  = treeNodeMaxDepth;

            rootLink            = new QuadTreeNodeLink();
            rayCastBroadLink    = new QuadTreeNodeLink();
            rayCastNarrowLink   = new QuadTreeNodeLink();
            aabbQueryBroadLink  = new QuadTreeNodeLink();
            aabbQueryNarrowLink = new QuadTreeNodeLink();

            bodies            = new BodyList();
            contactListLookup = new Dictionary<int, ContactList>();
            contactLists      = new List<ContactList>();

            broad = new BroadPhaseContactSolver();
        }

        #region Event handlers
        private void Tree_Removed(object sender, BodyEventArgs e)
            => Removed?.Invoke(this, e);

        private void Tree_Added(object sender, BodyEventArgs e)
            => Added?.Invoke(this, e);
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplySeparation(ref Body body, in NarrowContactSolverResult contact, TimeSpan delta)
        {
            body.ForcedPosition = contact.Translation;

            body.ApplyLinearForces(delta);
            body.TransformLinearForces();
            body.ResetForces();
        }

        private void ApplyConstantForces(int bodyId)
        {
            ref var body = ref bodies.WithId(bodyId);

            // Apply gravity and wind forces.
            body.ApplyLinearImpulse(Wind, Vector2.UnitX);
            body.ApplyLinearImpulse(Gravity, Vector2.UnitY);
        }

        private void ApplyConstantForces(QuadTreeNode node, TimeSpan delta)
        {
            if (node.IsSplit)
            {
                ApplyForces(node.TopLeft, delta);
                ApplyForces(node.TopRight, delta);
                ApplyForces(node.BottomRight, delta);
                ApplyForces(node.BottomLeft, delta);

                return;
            }

            foreach (var bodyId in node.Dynamics.Concat(node.Sensors))
                ApplyConstantForces(bodyId);
        }

        private void ApplyForces(int bodyId, TimeSpan delta)
        {
            ref var body = ref bodies.WithId(bodyId);

            if (!body.IsActive())
                return;

            // Apply angular and linear forces.
            var angularApplied = body.ApplyAngularForces(delta);
            var linearApplied  = body.ApplyLinearForces(delta);

            if (angularApplied)
                body.TransformAngularForces();
            else if (linearApplied)
                body.TransformLinearForces();

            body.ResetForces();

            Moved?.Invoke(this, new BodyEventArgs(bodyId));
        }

        private void ApplyForces(QuadTreeNode node, TimeSpan delta)
        {
            if (node.IsSplit)
            {
                ApplyForces(node.TopLeft, delta);
                ApplyForces(node.TopRight, delta);
                ApplyForces(node.BottomRight, delta);
                ApplyForces(node.BottomLeft, delta);

                return;
            }

            foreach (var bodyId in node.Dynamics.Concat(node.Sensors))
                ApplyForces(bodyId, delta);

            foreach (var bodyId in node.Statics)
                ApplyForces(bodyId, delta);
        }

        private void RelocateLostBody(int bodyId, TimeSpan delta)
        {
            // Compute distance between bounding box points.
            ref var body = ref bodies.WithId(bodyId);

            var bb = body.BoundingBox;
            var tb = tree.BoundingBox;

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
            if (dr < 0.0f) tr.X      += dr;
            else if (dl < 0.0f) tr.X += dl;

            // Overlap top or bottom.
            if (dt < 0.0f) tr.Y      += dt;
            else if (db < 0.0f) tr.Y += dt;

            // Apply overlap translation and update.
            body.ForcedPosition = body.Position + tr;

            body.ApplyLinearForces(delta);
            body.TransformLinearForces();
            body.ResetForces();

            // Should never happen.
            if (!tree.Add(body.Id))
                throw new InvalidOperationException("could not add lost body to world");

            Moved?.Invoke(this, new BodyEventArgs(bodyId));
        }

        public void Resize(Vector2 bounds)
        {
            if (bounds.X <= 0.0f || bounds.Y <= 0.0f)
                throw new InvalidOperationException("both components of size must be greater than zero and positive");

            tree = new QuadTree(bodies, treeNodeBodyLimit, treeNodeMaxDepth, bounds);

            for (var i = 0; i < bodies.Count; i++)
                tree.Add(bodies.AtIndex(i).Id);

            tree.Added   += Tree_Added;
            tree.Removed += Tree_Removed;
        }

        public int Create(BodyType type,
                          in Vector2 position,
                          float rotation,
                          in Shape shape,
                          object userData = null)
        {
            var bodyId = bodies.Create(type, position, rotation, shape, userData);

            if (!tree.Add(bodyId))
                RelocateLostBody(bodyId, TimeSpan.Zero);

            // Static bodies should not have contact lists.
            if (type == BodyType.Static)
                return bodyId;

            var contactList = new ContactList(bodyId);

            contactLists.Add(contactList);
            contactListLookup.Add(bodyId, contactList);

            return bodyId;
        }

        public int Create(BodyType type, in Vector2 position, in Shape shape, object userData = null)
            => Create(type, position, 0.0f, shape, userData);

        public int Create(BodyType type, in Shape shape, object userData = null)
            => Create(type, Vector2.Zero, 0.0f, shape, userData);

        public void Delete(int bodyId)
        {
            if (!tree.Remove(bodyId))
                throw new InvalidOperationException("could not delete body");

            ref var body = ref bodies.WithId(bodyId);

            if (body.Type != BodyType.Static)
            {
                contactLists.Remove(contactListLookup[bodyId]);
                contactListLookup.Remove(bodyId);
            }

            bodies.Delete(bodyId);
        }

        public void RootQuery(QuadTreeNodeLink link)
        {
            link.Clear();

            tree.RootQuery(link);
        }

        public void RayCastBroad(QuadTreeNodeLink link, in Line line, BodySelector selector = null)
        {
            link.Clear();

            tree.RayCastBroad(link, line, selector);
        }

        public void RayCastNarrow(QuadTreeNodeLink link, in Line line)
        {
            link.Clear();

            tree.RayCastNarrow(link, line);
        }

        public void AabbQueryBroad(QuadTreeNodeLink link, in Aabb aabb, BodySelector selector = null)
        {
            link.Clear();

            tree.AabbQueryBroad(link, aabb, selector);
        }

        public void AabbQueryNarrow(QuadTreeNodeLink link, in Aabb aabb)
        {
            link.Clear();

            tree.AabbQueryNarrow(link, aabb);
        }

        public QuadTreeNodeLink RootQuery()
        {
            RootQuery(rootLink);

            return rootLink;
        }

        public QuadTreeNodeLink RayCastBroad(in Line line, BodySelector selector = null)
        {
            RayCastBroad(rayCastBroadLink, line, selector);

            return rayCastBroadLink;
        }

        public QuadTreeNodeLink RayCastNarrow(in Line line)
        {
            RayCastNarrow(rayCastNarrowLink, line);

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
            => contactListLookup.TryGetValue(bodyId, out var contactList) ? contactList.CurrentBodyIds : Enumerable.Empty<int>();

        public override void Update(IGameEngineTime time)
        {
            if (tree == null)
                return;

            var delta = FixedTimeStep != TimeSpan.Zero ? FixedTimeStep : time.Elapsed;

            // Steps for running the so called simulation are as follows:
            // 
            // 1) apply forces
            // 2) sweep the quad tree, reposition "lost" bodies
            // 3) do broad phase pairing for bodies
            // 4) do narrow phase checks for bodies
            // 5) apply MTV translations
            // 6) update contact lists 
            // 7) invoke contact events

            // Add constant world gravity and wind forces.
            ApplyConstantForces(tree.Root, delta);

            // Sweep quad tree and re-position lost bodies.
            foreach (var bodyId in tree.RelocateLostBodies())
                RelocateLostBody(bodyId, delta);

            // Normalize user transformations before solve.
            ApplyForces(tree.Root, delta);

            // Solve all broad pairs.
            broad.Solve(tree, bodies);

            // Solve all narrow pairs.
            while (broad.Count != 0)
            {
                // Narrow solve pair.
                ref var pair       = ref broad.Next();
                ref var firstBody  = ref bodies.WithId(pair.FirstBodyId);
                ref var secondBody = ref bodies.WithId(pair.SecondBodyId);

                var contact = NarrowContactSolver.Solve(firstBody, secondBody);

                if (!contact.Collides)
                    continue;

                // Update contacts.
                var firstBodyContactList  = contactListLookup[pair.FirstBodyId];
                var secondBodyContactList = contactListLookup[pair.SecondBodyId];

                firstBodyContactList.Add(contact.SecondBodyId, frame, contact.Translation);
                secondBodyContactList.Add(contact.FirstBodyId, frame, contact.Translation);

                // Separate bodies.
                if (firstBody.Type == BodyType.Dynamic)
                    ApplySeparation(ref firstBody, contact, delta);
                else
                    ApplySeparation(ref secondBody, contact, delta);
            }

            // Invoke all contact events.
            for (var i = 0; i < contactLists.Count; i++)
            {
                // Invoke all begin contact events.
                foreach (var enteringBody in contactLists[i].EnteringBodyIds)
                    BeginContact?.Invoke(this, new BodyContactEventArgs(contactLists[i].BodyId, enteringBody, contactLists[i].GetTranslation(enteringBody)));

                // Invoke all end contact events.
                foreach (var leavingBody in contactLists[i].LeavingBodyIds)
                    EndContact?.Invoke(this, new BodyContactEventArgs(contactLists[i].BodyId, leavingBody, contactLists[i].GetTranslation(leavingBody)));
            }

            // Advance frame to keep contact lists in check.
            frame++;
        }

        public void Clear()
        {
            while (bodies.Count != 0)
                Delete(bodies.AtIndex(0).Id);
        }
    }
}