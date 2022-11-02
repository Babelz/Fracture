using System;
using Fracture.Common;
using Fracture.Engine.Core;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Enumeration that contains supported wrap modes of children.
    /// </summary>
    public enum WrapMode : byte
    {
        /// <summary>
        /// Children are wrapped horizontally across the x-axis. 
        /// </summary>
        Horizontal = 0,

        /// <summary>
        /// Children are wrapped vertically across the y-axis.
        /// </summary>
        Vertical = 1,
    }

    /// <summary>
    /// Invisible container that automatically positions children in vertical or horizontal manner.
    /// Does not support layout for elements of varying size.
    /// </summary>
    public sealed class WrapPanel : DynamicContainerControl
    {
        #region Fields
        private WrapMode mode;

        /// <summary>
        /// Is the wrap panel dirty because some controls layout changed.
        /// </summary>
        private bool dirty;
        #endregion

        #region Properties
        /// <summary>
        /// How much space is left between controls.
        /// </summary>
        public float SpaceBetween
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current wrap mode.
        /// </summary>
        public WrapMode Mode
        {
            get => mode;
            set
            {
                mode = value;

                WrapChildren();
            }
        }
        #endregion

        public WrapPanel()
            // Default to 1.0 size.
            => Size = Vector2.One;

        #region Event handlers
        private void Control_LayoutChanged(object sender, EventArgs e)
            => dirty = true;
        #endregion

        private void WrapChildrenHorizontal()
        {
            // Offset used to keep the children in middle of the panel in.
            // If children count is even, we center the children around the center.
            // If children count is odd, we center the children around a center children element.
            var offset = Children.ControlsCount % 2 == 0 ? 0.5f : 0.0f;
            var origin = Children[0].Size.X;

            for (var i = 0; i < Children.ControlsCount; i++)
            {
                Children[i].Positioning = Positioning.Relative;

                Children[i].Position = new Vector2(0.5f - Children[i].Size.X * 0.5f + offset * Children[i].Size.X + offset * SpaceBetween,
                                                   0.5f - Children[i].Size.Y * 0.5f);

                Children[i].Margin = i % 2 == 0 ? UiOffset.ToLeft((origin + SpaceBetween) * i / 2) : UiOffset.ToLeft((-origin + -SpaceBetween) * (i + 1) / 2);
            }
        }

        private void WrapChildrenVertical()
        {
            // Offset used to keep the children in middle of the panel in.
            // If children count is even, we center the children around the center.
            // If children count is odd, we center the children around a center children element.
            var offset = Children.ControlsCount % 2 == 0 ? 0.5f : 0.0f;
            var origin = Children[0].Size.Y;

            for (var i = 0; i < Children.ControlsCount; i++)
            {
                Children[i].Positioning = Positioning.Relative;

                Children[i].Position = new Vector2(0.5f - Children[i].Size.X * 0.5f,
                                                   0.5f -
                                                   Children[i].Size.Y * 0.5f +
                                                   offset * Children[i].Size.Y +
                                                   offset * SpaceBetween);

                Children[i].Margin = i % 2 == 0 ? UiOffset.ToTop((origin + SpaceBetween) * i / 2) : UiOffset.ToTop((-origin + -SpaceBetween) * (i + 1) / 2);
            }
        }

        private void WrapChildren()
        {
            if (ControlsCount == 0)
                return;

            switch (mode)
            {
                case WrapMode.Horizontal:
                    WrapChildrenHorizontal();

                    break;
                case WrapMode.Vertical:
                    WrapChildrenVertical();

                    break;
                default:
                    throw new InvalidOrUnsupportedException(nameof(WrapMode), mode);
            }
        }

        protected override void InternalUpdate(IGameEngineTime time)
        {
            if (dirty)
            {
                // Wrap children to the container if the 
                // container children have changed layout.
                WrapChildren();

                dirty = false;
            }

            base.InternalUpdate(time);
        }

        public override void Add(IControl control)
        {
            base.Add(control);

            control.LayoutChanged += Control_LayoutChanged;

            dirty = true;
        }

        public override void Remove(IControl control)
        {
            base.Remove(control);

            control.LayoutChanged += Control_LayoutChanged;

            dirty = true;
        }

        public override void UpdateLayout()
        {
            base.UpdateLayout();

            dirty = true;
        }
    }
}