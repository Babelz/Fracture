using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Graphics
{
    /// <summary>
    /// Interface for implementing 2D cameras.
    /// </summary>
    public interface IView
    {
        #region Properties
        int Id
        {
            get;
        }

        Aabb BoundingBox
        {
            get;
        }

        Vector2 Bounds
        {
            get;
        }

        Vector2 Position
        {
            get;
        }

        float Rotation
        {
            get;
        }

        float Zoom
        {
            get;
        }

        Matrix Matrix
        {
            get;
        }

        Viewport Viewport
        {
            get;
            set;
        }
        #endregion

        void ScrollTo(in Vector2 position);
        void ScrollBy(in Vector2 amount);

        void RotateTo(float rotation);
        void RotateBy(float amount);

        void FocusTo(float zoom);
        void FocusBy(float amount);

        /// <summary>
        /// Returns screen space point in view space.
        /// </summary>
        Vector2 ScreenToWorld(in Point screenPosition);

        /// <summary>
        /// Returns screen space vector in view space.
        /// </summary>
        Vector2 ScreenToWorld(in Vector2 screenPosition);

        /// <summary>
        /// Updates view matrix of the view. Should be called after
        /// the view has changed.
        /// </summary>
        void Update();
    }

    /// <summary>
    /// Default implementation of <see cref="IView"/>. Views are
    /// always positioned around their center.
    /// </summary>
    public class View : IView
    {
        #region Properties
        public int Id
        {
            get;
        }

        public Aabb BoundingBox
        {
            get;
            private set;
        }

        public Vector2 Bounds
        {
            get;
            private set;
        }

        public Vector2 Position
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the rotation of the camera in radians.
        /// </summary>
        public float Rotation
        {
            get;
            private set;
        }

        public float Zoom
        {
            get;
            private set;
        }

        public Matrix Matrix
        {
            get;
            private set;
        }

        public Viewport Viewport
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Creates new instance of view using given view port and sets its
        /// origin to the center of the viewport.
        /// </summary>
        public View(int id, in Viewport viewport)
        {
            Id       = id;
            Viewport = viewport;
            Position = Vector2.Zero;
            Zoom     = 1.0f;
            Rotation = 0.0f;

            Update();
        }

        public void ScrollTo(in Vector2 position)
        {
            Position = position;

            Update();
        }

        public void ScrollBy(in Vector2 amount)
        {
            Position += amount;

            Update();
        }

        public void RotateTo(float rotation)
        {
            Rotation = rotation;

            Update();
        }

        public void RotateBy(float amount)
        {
            Rotation += amount;

            Update();
        }

        public void FocusTo(float zoom)
        {
            Zoom = zoom;

            Update();
        }

        public void FocusBy(float amount)
        {
            Zoom += amount;

            Update();
        }

        /// <summary>
        /// Translates given point on screen space to camera space.
        /// </summary>
        public Vector2 ScreenToWorld(in Vector2 point)
            => Transform.ToWorldUnits(Vector2.Transform(point, Matrix.Invert(Matrix)));

        /// <summary>
        /// Translates given point on screen space to camera space.
        /// </summary>
        public Vector2 ScreenToWorld(in Point point)
            => ScreenToWorld(new Vector2(point.X, point.Y));

        public void Update()
        {
            Bounds = new Vector2(Viewport.Width / Zoom, Viewport.Height / Zoom);

            BoundingBox = new Aabb(Position, Rotation, Bounds);
            Matrix      = CreateViewMatrix(BoundingBox.Position, new Vector2(Viewport.Width, Viewport.Height), Rotation, Zoom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix CreateViewMatrix(in Vector2 position, in Vector2 bounds, float rotation, float zoom)
            => Matrix.CreateTranslation(new Vector3(-position.X, -position.Y, 0.0f)) *
               Matrix.CreateRotationZ(rotation) *
               Matrix.CreateScale(new Vector3(zoom, zoom, 1.0f)) *
               Matrix.CreateTranslation(new Vector3(bounds.X * 0.5f, bounds.Y * 0.5f, 0.0f));
    }

    /// <summary>
    /// Interface for implementing view systems that manage views
    /// and supports iterating over them.
    /// </summary>
    public interface IViewSystem : IObjectManagementSystem, IEnumerable<IView>
    {
        IView Create(in Viewport viewport);

        void Delete(IView view);
    }

    /// <summary>
    /// Default implementation of <see cref="IViewSystem"/>.
    /// </summary>
    public sealed class ViewSystem : GameEngineSystem, IViewSystem
    {
        #region Fields
        private readonly FreeList<int> ids;

        private readonly List<IView> views;
        #endregion

        [BindingConstructor]
        public ViewSystem()
        {
            var idc = 0;

            ids   = new FreeList<int>(() => idc++);
            views = new List<IView>();
        }

        public IView Create(in Viewport viewport)
        {
            var view = new View(ids.Take(), viewport);

            views.Add(view);

            return view;
        }

        public void Delete(IView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (!views.Remove(view))
                throw new InvalidOperationException($"view {view.Id} doest not exist");

            ids.Return(view.Id);
        }

        public void Clear()
        {
            while (views.Count != 0)
                Delete(views[0]);
        }

        public IEnumerator<IView> GetEnumerator()
            => views.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}