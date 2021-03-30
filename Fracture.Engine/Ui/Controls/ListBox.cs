using System;
using System.Collections.Generic;
using System.Linq;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Fracture.Engine.Input.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shattered.Content.Ui;

namespace Fracture.Engine.Ui.Controls
{
    public sealed class ListBoxSelectionChangedEventArgs : EventArgs
    {
        #region Properties
        public int OldIndex
        {
            get;
        }
        public int NewIndex
        {
            get;
        }

        public IListBoxItem OldItem
        {
            get;
        }
        public IListBoxItem NewItem
        {
            get;
        }
        #endregion

        public ListBoxSelectionChangedEventArgs(int oldIndex, int newIndex, IListBoxItem oldItem, IListBoxItem newItem)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;

            OldItem = oldItem;
            NewItem = newItem;
        }
    }
    
    /// <summary>
    /// Interface for implementing list box items.
    /// </summary>
    public interface IListBoxItem
    {
        #region Properties
        /// <summary>
        /// Name of the item, this will be displayed
        /// on the list box.
        /// </summary>
        string Name
        {
            get;
        }
        #endregion
    }
    
    /// <summary>
    /// Generic list box item bound by name and user data.
    /// </summary>
    public sealed class ListBoxItem : IListBoxItem
    {
        #region Fields
        public string Name
        {
            get;
        }

        /// <summary>
        /// User data associated to this item.
        /// </summary>
        public object UserData
        {
            get;
        }
        #endregion

        public ListBoxItem(string name, object userData)
        {
            Name     = name;
            UserData = userData;
        }
    }
    
    /// <summary>
    /// Control that contains list of selectable items.
    /// </summary>
    public sealed class ListBox : Control
    {
        #region Fields
        private Vector2 retractedSize;

        private readonly List<IListBoxItem> items;

        private bool collapsed;
        #endregion

        #region Events
        public event EventHandler<ListBoxSelectionChangedEventArgs> SelectionChanged;
        #endregion

        #region Properties
        public IEnumerable<IListBoxItem> Items => items;

        public IListBoxItem SelectedItem
        {
            get;
            private set;
        }

        public int SelectedIndex
        {
            get;
            private set;
        }

        public float ItemSeparator
        {
            get;
            set;
        }
        #endregion
        
        public ListBox(IEnumerable<IListBoxItem> items)
        {
            this.items = new List<IListBoxItem>(items ?? throw new ArgumentNullException(nameof(items)));

            Size = new Vector2(0.35f, 0.10f);

            Select(this.items.Count != 0 ? 0 : -1);
        }
        
        public ListBox()
            : this(new List<IListBoxItem>())
        {
        }

        private Vector2 GetCollapsedSize()
        {
            var size = Size;

            size.X = ActualSize.X;

            var itemSource = Style.Get<Rectangle>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Source.Text}");
            var tuples     = GetItemTuples();
            
            var itemAreaTop    = tuples.First().Rectangle.Bottom;
            var itemAreaBottom = tuples.Last().Rectangle.Bottom;
            var itemArea       = itemAreaBottom - itemAreaTop - itemSource.Y;

            size += UiCanvas.ToLocalUnits(itemArea * Vector2.UnitY);

            return size;
        }
        
        private Vector2 GetBackgroundScale()
        {
            var background = Style.Get<Texture2D>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Texture.Normal}");
            var area       = GetRenderDestinationRectangle();
            
            return GraphicsUtils.ScaleToTarget(new Vector2(background.Width, background.Height),
                                                      new Vector2(area.Width, area.Height));
        }

        private (Rectangle Rectangle, IListBoxItem Item)[] GetItemTuples()
        {
            var font       = Style.Get<SpriteFont>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Font.Normal}");
            var itemSource = Style.Get<Rectangle>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Source.Text}");
            
            var areas           = new (Rectangle Rectangle, IListBoxItem Item)[items.Count];
            var separator       = (int)Math.Floor(UiCanvas.ToScreenUnits(ItemSeparator * Vector2.UnitY).Y);
            var backgroundScale = GetBackgroundScale();
            var area            = GetRenderDestinationRectangle();
            var i               = 0;
            
            var itemArea = UiUtils.ScaleTextArea(new Vector2(area.X, area.Y), backgroundScale, itemSource);

            foreach (var item in items)
            {
                var size = font.MeasureString(item.Name);

                areas[i].Rectangle = itemArea;
                
                if (i > 0) areas[i].Rectangle.Y = areas[i - 1].Rectangle.Bottom + separator;
                else       areas[i].Rectangle.Y = itemArea.Y;

                areas[i].Rectangle.Width  = (int)Math.Floor(size.X);
                areas[i].Rectangle.Height = (int)Math.Floor(size.Y);
                
                areas[i++].Item = item;
            }

            return areas;
        }
        
        protected override void InternalReceiveMouseInput(IGameEngineTime time, IMouseDevice mouse)
        {
            if (!HasFocus) return;
            
            // If we have focus and we are clicked, collapse.
            if (!Mouse.IsPressed(MouseButton.Left)) return;
            
            // If we have focus and we are clicked and we are collapsed, resolve selected item 
            // and retract.
            if (collapsed) Retract();
            else           Collapse();
        }

        protected override void UpdateSize(bool actual = false)
        {
            base.UpdateSize(actual);

            if (!collapsed) retractedSize = Size;
        }

        protected override void UpdatePosition(bool actual = false)
        {
            if (!collapsed)
                base.UpdatePosition(actual);
        }

        public override void Defocus()
        {
            base.Defocus();

            if (collapsed) Retract();
        }

        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            // Control style related values.
            var background = Style.Get<Texture2D>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Texture.Normal}");
            var arrow      = Style.Get<Texture2D>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Texture.Arrow}");
            var font       = Style.Get<SpriteFont>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Font.Normal}");
            var color      = Style.Get<Color>($"{UiStyleKeys.Target.ListBox}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");

            // Item style related values.
            var itemSelectedColor = Style.Get<Color>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Color.ItemSelected}");
            var itemNormalColor   = Style.Get<Color>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Color.ItemNormal}");
            var itemHoverColor    = Style.Get<Color>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Color.ItemHover}");
            var itemSource        = Style.Get<Rectangle>($"{UiStyleKeys.Target.ListBox}\\{UiStyleKeys.Source.Text}");

            var destination     = GetRenderDestinationRectangle();
            var backgroundScale = GetBackgroundScale();
            
            // If we are collapsed, draw arrow flipped horizontally.
            var effect = collapsed ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            // Draw box background.
            fragment.DrawSprite(new Vector2(destination.X, destination.Y),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero,
                                new Vector2(destination.Width, destination.Height),
                                background,
                                color);
            
            if (collapsed && items.Count != 0)
            {
                // Draw item we are hovering over highlighted with highlight color. 
                foreach (var tuple in GetItemTuples())
                {
                    Color activeColor;

                    if      (ReferenceEquals(SelectedItem, tuple.Item)) activeColor = itemSelectedColor;
                    else if (Mouse.IsHovering(tuple.Rectangle))         activeColor = itemHoverColor;
                    else                                                activeColor = itemNormalColor;
                    
                    fragment.DrawSpriteText(new Vector2(tuple.Rectangle.X, tuple.Rectangle.Y),
                                            Vector2.One, 
                                            0.0f,
                                            Vector2.Zero,
                                            tuple.Item.Name,
                                            font,
                                            activeColor);
                }
            }
            else
            {
                if (SelectedItem != null)
                {
                    // Draw selected item.
                    var itemArea     = UiUtils.ScaleTextArea(new Vector2(destination.X, destination.Y), backgroundScale, itemSource);
                    var textCenter   = destination.Height / 2.0f - font.MeasureString(SelectedItem.Name).Y * 0.5f;
                    var textPosition = new Vector2(itemArea.X, destination.Y);

                    fragment.DrawSpriteText(textPosition + textCenter * Vector2.UnitY,
                                            Vector2.One,
                                            0.0f,
                                            Vector2.Zero,
                                            SelectedItem.Name,
                                            font,
                                            itemSelectedColor);
                }
            }
            
            // Draw arrow.
            fragment.DrawSprite(new Vector2(destination.Right - arrow.Width, destination.Top),
                                Vector2.One,
                                0.0f,
                                Vector2.Zero, 
                                new Vector2(arrow.Width, arrow.Height),
                                arrow,
                                color);
        }
        
        public void Add(IListBoxItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            items.Add(item);
        }
        public bool Remove(IListBoxItem item)
        {
            var removed = items.Remove(item);
            
            if (SelectedIndex != -1 && ReferenceEquals(item, SelectedItem))
                Select(-1);

            return removed;
        }

        public void Insert(IListBoxItem item, int index)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            items.Insert(index, item);
        }
        public void RemoveAt(int index)
        {
            if (ReferenceEquals(SelectedItem, items[index])) Select(-1);

            items.RemoveAt(index);
        }

        public void Clear()
        {
            while (items.Count != 0)
                RemoveAt(0);
        }

        public void Select(int index)
        {
            if (index < 0)
            {
                SelectedIndex = -1;
                SelectedItem  = null;

                return;
            }

            var oldSelectedIndex = SelectedIndex;
            var oldSelectedItem  = SelectedItem;

            SelectedIndex = index;
            SelectedItem  = items[index];

            SelectionChanged?.Invoke(this, new ListBoxSelectionChangedEventArgs(oldSelectedIndex, SelectedIndex, oldSelectedItem, SelectedItem));
        }

        public void Select(IListBoxItem item)
        {
            if (item == null)
            {
                Select(-1);

                return;
            }

            Select(items.IndexOf(item));
        }

        public void Retract()
        {
            // If no item is clicked, retract and use the currently selected item.
            var tuples = GetItemTuples();
            
            foreach (var tuple in tuples)
            {
                if (!Mouse.IsHovering(tuple.Rectangle)) continue;
                
                Select(tuple.Item);

                break;
            }

            collapsed = false;
            Size      = retractedSize;
        }

        public void Collapse()
        {
            if (items.Count == 0)
                return;

            collapsed = true;

            retractedSize = Size;
            ActualSize    = GetCollapsedSize();
        }
    }
}
