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
    public sealed class ListViewColumnEventArgs : EventArgs
    {
        #region Properties
        public ListViewColumnDefinition Column
        {
            get;
        }
        #endregion

        public ListViewColumnEventArgs(ListViewColumnDefinition column)
            => Column = column;
    }

    public sealed class ListViewItemEventArgs : EventArgs
    {
        #region Properties
        public IListViewItem Item
        {
            get;
        }
        #endregion

        public ListViewItemEventArgs(IListViewItem item)
            => Item = item;
    }
    
    public sealed class ListViewColumnDefinition
    {
        #region Properties
        public Vector2 Size
        {
            get;
        }
        public string Name
        {
            get;
        }
        #endregion

        public ListViewColumnDefinition(Vector2 size, string name)
        {
            Size = size;
            Name = name;
        }
    }
    
    public interface IListViewItem
    {
        #region Events
        event EventHandler UserDataChanged;
        #endregion

        #region Properties
        IEnumerable<IControl> ColumnControls
        {
            get;
        }
        IEnumerable<string> ColumnNames
        {
            get;
        }

        object UserData
        {
            get;
            set;
        }
        #endregion

        IControl GetColumnControl(string column);

        void UpdateUserData();
    }
    
    public class ListViewItem : IListViewItem
    {
        #region Fields
        private readonly IDictionary<string, IControl> columns;

        private object userData;
        #endregion
        
        #region Events
        public event EventHandler UserDataChanged;
        #endregion

        #region Properties
        public object UserData
        {
            get => userData;
            set
            {
                if (userData == value)
                    return;

                userData = value;
                
                UserDataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IEnumerable<IControl> ColumnControls => columns.Values;

        public IEnumerable<string> ColumnNames => columns.Keys;
        #endregion

        public ListViewItem(IDictionary<string, IControl> columns)
            => this.columns = columns ?? throw new ArgumentNullException(nameof(columns));

        public ListViewItem(IDictionary<string, IControl> columns, object userData)
            : this(columns) => this.userData = userData;
        
        public IControl GetColumnControl(string column)
            => columns[column];
        
        public void UpdateUserData()
            => UserDataChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public sealed class ListView : StaticContainerControl
    {
        #region Fields
        private readonly List<IListViewItem> items;

        private readonly List<ListViewColumnDefinition> columns;

        private readonly List<Button> columnButtons;
        private readonly ContentRoot itemsContent;
        
        private readonly ImageBox selectedImageBox;
        private readonly ImageBox hoverImageBox;

        private IListViewItem selectedItem;
        #endregion

        #region Events
        public event EventHandler<ListViewColumnEventArgs> ColumnClicked;

        public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;
        #endregion

        #region Properties
        public int ColumnsCount => columns.Count;
        public int RowsCount    => items.Count;

        public IEnumerable<IListViewItem> Items => items;

        public IListViewItem SelectedItem
        {
            get => selectedItem;
            private set
            {
                selectedItem = value;
                
                SelectedItemChanged?.Invoke(this, new ListViewItemEventArgs(SelectedItem));
            }
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

        public Vector2 ItemsContentViewOffset
        {
            get => itemsContent.ViewOffset;
            set => itemsContent.ViewOffset = value;
        }

        public Vector2 ItemsContentViewSafePosition => itemsContent.ViewSafePosition;

        public override IUiStyle Style
        {
            get => base.Style;
            set
            {
                base.Style = value;

                if (value == null) return;

                hoverImageBox.Image    = value.Get<Texture2D>($"{UiStyleKeys.Target.ListView}\\{UiStyleKeys.Texture.Hover}");
                selectedImageBox.Image = value.Get<Texture2D>($"{UiStyleKeys.Target.ListView}\\{UiStyleKeys.Texture.Focused}");

                hoverImageBox.ImageColor    = value.Get<Color>($"{UiStyleKeys.Target.ListView}\\{UiStyleKeys.Color.ItemHover}");
                selectedImageBox.ImageColor = value.Get<Color>($"{UiStyleKeys.Target.ListView}\\{UiStyleKeys.Color.ItemSelected}");
            }
        }
        #endregion

        public ListView()
        {
            columns = new List<ListViewColumnDefinition>();
            items   = new List<IListViewItem>();

            columnButtons = new List<Button>();

            Size = new Vector2(0.9f);

            itemsContent = new ContentRoot
            {
                Id              = "items-content",
                Parent          = this,
                Size            = new Vector2(1.0f),
                Positioning     = Positioning.Relative,
                ViewOffset      = new Vector2(0.0f, 0.0f),
                UseRenderTarget = true,
            };

            hoverImageBox = new ImageBox
            {
                ImageMode = ImageMode.Fit
            };

            selectedImageBox = new ImageBox
            {
                ImageMode = ImageMode.Fit
            };

            hoverImageBox.Hide();
            selectedImageBox.Hide();
            
            Children.Add(itemsContent);

            UpdateLayout();

            MouseInputReceived += ListView_MouseInputReceived;

            SelectedIndex = -1;
        }
        
        #region Event handlers
        private void ListView_MouseInputReceived(object sender, ControlMouseInputEventArgs e)
        {
            if (RowsCount == 0) return;

            // Update selected item by mouse position.
            if (Mouse.IsHovering(itemsContent))
            {
                // Compute actual item area used by the content.
                var itemsArea = UiCanvas.ToVerticalScreenUnits(itemsContent.Controls.Max(c => c.ActualBoundingBox.Bottom) - 
                                                                          itemsContent.Controls.Min(c => c.ActualBoundingBox.Top));
                
                // Get rows and item size in the content.
                var rows      = items.Count;
                var itemSize  = itemsArea / rows;

                // Compute the actual index where the mouse is pointing.
                var index = (int)(((e.MouseInputManager.CurrentScreenPosition - UiCanvas.ToScreenUnits(itemsContent.ActualPosition)).Y + 
                                  UiCanvas.ToVerticalScreenUnits(ItemsContentViewOffset.Y)) / itemSize);
                
                // If index is valid, transform index to control index in the items content.
                if (index < 0 || index >= RowsCount) return;
                
                var controlIndex = index * ColumnsCount;
                var control      = itemsContent.Controls.ElementAt(controlIndex);

                if (e.MouseInputManager.IsPressed(MouseButton.Left))
                {
                    SelectedIndex = index;
                    SelectedItem  = items[index];

                    UpdateSelectedPosition(control);
                }
                    
                UpdateHoverPosition(control);
            }
            else
                UpdateHoverPosition(null);
        }

        private void Button_Click(object sender, EventArgs e)
            => ColumnClicked?.Invoke(this, new ListViewColumnEventArgs((ListViewColumnDefinition)(sender as IControl)!.UserData));
        #endregion

        private void UpdateSelectedPosition(IControl control)
        {
            if (control == null)
            {
                selectedImageBox.Hide();

                return;   
            }
            
            // Update position based on control position.
            selectedImageBox.ActualPosition = new Vector2(ActualPosition.X, control.ActualPosition.Y);

            selectedImageBox.Show();
        }

        private void UpdateHoverPosition(IControl control)
        {
            if (control == null)
            {
                hoverImageBox.Hide();

                return;
            }

            // Update position based on control position.
            hoverImageBox.ActualPosition = new Vector2(ActualPosition.X, control.ActualPosition.Y);

            hoverImageBox.Show();
        }

        private void UpdateColumnButtonPositions()
        {
            if (columnButtons.Count == 0) return;

            var lastButton = columnButtons.First();

            lastButton.Position = Vector2.Zero;

            foreach (var button in columnButtons.Skip(1))
            {
                button.Position = new Vector2(lastButton.BoundingBox.Right, 0.0f);

                lastButton = button;
            }
        }

        private void ReconstructColumnsView()
        {
            // Remove current columns from view.
            foreach (var button in columnButtons)
            {
                button.Click -= Button_Click;

                Children.Remove(button);
            }

            columnButtons.Clear();

            // Add new columns to the view.
            var minX = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var column in columns)
            {
                var button = new Button
                {
                    Id          = $"column-button-{column.Name}",
                    Text        = column.Name,
                    Size        = new Vector2(column.Size.X, 0.05f),
                    UserData    = column,
                    Positioning = Positioning.Relative,
                    Position    = Vector2.Zero
                };

                button.Click += Button_Click;

                columnButtons.Add(button);
                Children.Add(button);

                if (button.ActualBoundingBox.Left < minX)   minX = button.BoundingBox.Left;
                if (button.ActualBoundingBox.Bottom > maxY) maxY = button.BoundingBox.Bottom;
            }

            // Update items content position and size.
            itemsContent.Position = new Vector2(minX, maxY);
            itemsContent.Size     = new Vector2(1.0f, 1.0f - maxY);

            var imageBoxSizes = new Vector2(1.0f, columns.Max(c => c.Size.Y));

            hoverImageBox.Size    = imageBoxSizes;
            selectedImageBox.Size = imageBoxSizes;

            UpdateColumnButtonPositions();
        }
        
        private void ReconstructItemsView()
        {
            // Clear current contents from items content.
            itemsContent.Clear();

            // Generate new items and place them.
            var offsetY = 0.0f;

            foreach (var item in items)
            {
                var offsetX = 0.0f;
                var maxY    = 0.0f;

                foreach (var column in columns)
                {
                    var control = item.GetColumnControl(column.Name);
                    
                    if (control == null)
                        throw new InvalidOperationException($"item does not contain control for column {column.Name}");
                    
                    control.Id          = "dynamic-view-content";
                    control.Positioning = Positioning.Relative;
                    control.Position    = new Vector2(offsetX, offsetY);
                    control.Size        = column.Size;
                    control.UserData    = item;
                    
                    itemsContent.Add(control);
                    itemsContent.UpdateLayout();
                    
                    offsetX += column.Size.X;

                    if (column.Size.Y > maxY)
                        maxY = column.Size.Y;
                }

                offsetY += maxY + ItemSeparator;
            }

            // Clear current selection if it does not exist in the collection any more.
            itemsContent.Add(hoverImageBox);
            itemsContent.Add(selectedImageBox);

            if (items.Contains(SelectedItem))
            {
                SelectedIndex = items.IndexOf(SelectedItem);
            }
            else
            {
                SelectedIndex = -1;
                SelectedItem  = null;
            }
            
            UpdateHoverPosition(null);
            UpdateSelectedPosition(null);
        }

        protected override void InternalDraw(IGraphicsFragment fragment, IGameEngineTime time)
        {
            var texture     = Style.Get<Texture2D>($"{UiStyleKeys.Target.Panel}\\{UiStyleKeys.Texture.Normal}");
            var color       = Style.Get<Color>($"{UiStyleKeys.Target.Panel}\\{(Enabled ? UiStyleKeys.Color.Enabled : UiStyleKeys.Color.Disabled)}");
            var center      = Style.Get<Rectangle>($"{UiStyleKeys.Target.Panel}\\{UiStyleKeys.Source.Center}");
            var destination = GetRenderDestinationRectangle();

            fragment.DrawSurface(texture, center, destination, color);
            
            base.InternalDraw(fragment, time);
        }

        public override void UpdateChildrenLayout()
        {
            base.UpdateChildrenLayout();

            UpdateColumnButtonPositions();
        }

        public void AddColumn(ListViewColumnDefinition column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            columns.Add(column);

            ReconstructColumnsView();
        }
        public bool RemoveColumn(ListViewColumnDefinition column)
        {
            var removed = columns.Remove(column);
            
            if (removed) ReconstructColumnsView();

            return removed;
        }

        public ListViewColumnDefinition ColumnAtIndex(int index)
            => columns[index];
        
        public void AddItem(IListViewItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            items.Add(item);

            ReconstructItemsView();
        }

        public bool RemoveItem(IListViewItem item)
        {
            var removed = items.Remove(item);

            if (removed) ReconstructItemsView();

            return removed;
        }

        public IListViewItem ItemAtIndex(int index)
            => items[index];

        public void SortItems(Comparison<IListViewItem> comparison)
        {
            items.Sort(comparison);

            ReconstructItemsView();

            SelectedIndex = -1;
        }
        
        public override void Clear()
        {
            base.Clear();
            
            ReconstructItemsView();
        }
    }
}