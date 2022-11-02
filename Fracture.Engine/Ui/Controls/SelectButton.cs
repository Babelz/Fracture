using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    /// <summary>
    /// Button that combines normal button and checkbox functionality.
    /// </summary>
    public sealed class SelectButton : Button
    {
        #region Events
        public event EventHandler SelectedChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Group of this select button. If the button belongs to a group,
        /// it will serve as a radio button.
        /// </summary>
        public IEnumerable<SelectButton> Group
        {
            get;
            private set;
        }

        /// <summary>
        /// Is button selected.
        /// </summary>
        public bool Selected
        {
            get;
            private set;
        }
        #endregion

        public SelectButton()
            => Size = new Vector2(0.2f);

        protected override Texture2D GetStyleStateTexture()
            => Selected ? Style.Get<Texture2D>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Texture.Hover}") : base.GetStyleStateTexture();

        protected override Point GetStyleStateOffset()
        {
            if (!Selected)
                return base.GetStyleStateOffset();

            var offset = Style.Get<Vector2>($"{UiStyleKeys.Target.Button}\\{UiStyleKeys.Offset.Click}");

            return new Point((int)Math.Floor(offset.X), (int)Math.Floor(offset.Y));
        }

        protected override void InternalClick()
        {
            Select(!Selected);

            base.InternalClick();
        }

        public void Select(bool selected = true)
        {
            if (Group != null)
            {
                foreach (var other in Group)
                {
                    if (ReferenceEquals(this, other))
                        continue;

                    other.Selected = false;
                }

                // Group requires one to be checked.
                if (Selected && !selected)
                    return;
            }

            var old = Selected;
            Selected = selected;

            if (old != Selected)
                SelectedChanged?.Invoke(this, EventArgs.Empty);
        }

        public void GroupWith(IEnumerable<SelectButton> group)
        {
            Group = group;

            Select();
        }
    }
}