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

        /// <summary>
        /// Gets the bounding box of the view in world space.
        /// </summary>
        Aabb BoundingBox
        {
            get;
        }

        /// <summary>
        /// Gets the view bounds in world space.
        /// </summary>
        Vector2 Bounds
        {
            get;
        }

        /// <summary>
        /// Gets the view position in world space.
        /// </summary>
        Vector2 Position
        {
            get;
        }

        /// <summary>
        /// Gets the rotation of the view in radians.
        /// </summary>
        float Rotation
        {
            get;
        }

        /// <summary>
        /// Gets the zoom level of the view.
        /// </summary>
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
        
        /// <summary>
        /// Gets the view area in world units.
        /// </summary>
        Rectf ViewArea
        {
            get;
        }
        #endregion

        void TransformPosition(in Vector2 transform);
        void TranslatePosition(in Vector2 translation);

        void TransformRotation(float transform);
        void TranslateRotation(float translation);

        void TransformZoom(float zoom);
        void TranslateZoom(float amount);

        void Clamp(in Rectf viewArea);
        
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
        
        public Rectf ViewArea
        {
            get;
            private set;
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

        public void TransformPosition(in Vector2 transform)
        {
            Position = transform;

            Update();
        }

        public void TranslatePosition(in Vector2 translation)
        {
            Position += translation;

            Update();
        }

        public void TransformRotation(float transform)
        {
            Rotation = transform;

            Update();
        }

        public void TranslateRotation(float translation)
        {
            Rotation += translation;

            Update();
        }

        public void TransformZoom(float zoom)
        {
            Zoom = zoom;

            Update();
        }

        public void TranslateZoom(float amount)
        {
            Zoom += amount;

            Update();
        }

        public void Clamp(in Rectf viewArea)
        {
            ViewArea = viewArea;
            
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
            Bounds      = new Vector2(Transform.ToWorldUnits(Viewport.Width) / Zoom, Transform.ToWorldUnits(Viewport.Height) / Zoom);
            BoundingBox = new Aabb(Position, Rotation, Bounds);
            
            // Ensure view is inside view area.
            if (ViewArea != Rectf.Empty)
            {
                // Clamp right and left.
                if (BoundingBox.Right > ViewArea.Right)
                    Position = new Vector2(ViewArea.Right - BoundingBox.HalfBounds.X, Position.Y);
                else if (BoundingBox.Left < ViewArea.Left)
                    Position = new Vector2(ViewArea.Left + BoundingBox.HalfBounds.X, Position.Y);
                
                // Clamp top and bottom.
                if (BoundingBox.Bottom > ViewArea.Bottom)
                    Position = new Vector2(Position.X, ViewArea.Bottom - BoundingBox.HalfBounds.Y);
                else if (BoundingBox.Top < ViewArea.Top)
                    Position = new Vector2(Position.X, ViewArea.Top + BoundingBox.HalfBounds.Y);
                
                // Recalculate bounding box.
                BoundingBox = new Aabb(Position, Rotation, Bounds);
            }
            
            Matrix = CreateViewMatrix(Transform.ToScreenUnits(BoundingBox.Position), new Vector2(Viewport.Width, Viewport.Height), Rotation, Zoom);
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