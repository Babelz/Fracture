using System;
using System.Collections.Generic;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Check box that can be used as normal checkbox or as a radio button
    /// when grouped with other checkboxes.
    /// </summary>
    public sealed class Checkbox : Button
    {
        #region Events
        public event EventHandler CheckedChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Group of this checkbox. If the button belongs to a group,
        /// it will serve as a radio button.
        /// </summary>
        public IEnumerable<Checkbox> Group
        {
            get;
            private set;
        }

        /// <summary>
        /// Is the checkbox checked.
        /// </summary>
        public bool Checked
        {
            get;
            private set;
        }
        #endregion

        public Checkbox() => Size = new Vector2(0.2f);

        protected override void InternalClick()
        {
            Check(!Checked);

            base.InternalClick();
        }

        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture     = Style.Get<Texture2D>($"{UiStyleKeys.Target.Checkbox}\\{(Checked ? UiStyleKeys.Texture.Checked : UiStyleKeys.Texture.Unchecked)}");
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.Checkbox}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var destination = GetRenderDestinationRectangle();
            var position    = new Vector2(destination.X, destination.Y);

            if (Mouse.IsHovering(this) && (Mouse.IsDown(MouseButton.Left) || Mouse.IsPressed(MouseButton.Left)))
            {
                var offset = Style.Get<Vector2>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Offset.Click}");

                position.X += (int)Math.Floor(offset.X);
                position.Y += (int)Math.Floor(offset.Y);
            }

            fragment.DrawSprite(position,
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(destination.Width, destination.Height),
                                texture,
                                color);
        }

        public void Check(bool isChecked = false)
        {
            if (Group != null)
            {
                foreach (var other in Group)
                {
                    if (ReferenceEquals(this, other))
                        continue;

                    other.Checked = false;
                }

                // Group requires one to be checked.
                if (Checked && !isChecked) return;
            }

            var old = Checked;
            Checked = isChecked;

            if (old != Checked)
                CheckedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void GroupWith(IEnumerable<Checkbox> group)
        {
            Group = group;

            Check(true);
        }
    }
}