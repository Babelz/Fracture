using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Util;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Ecs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog.Targets;

namespace Fracture.Engine.Graphics
{
    /// <summary>
    /// Event arguments used with graphics layers.
    /// </summary>
    public sealed class GraphicsElementLayerEventArgs : EventArgs
    {
        #region Properties
        public GraphicsElementLayer Layer
        {
            get;
        }
        #endregion

        public GraphicsElementLayerEventArgs(GraphicsElementLayer layer)
            => Layer = layer;
    }

    /// <summary>
    /// Structure that defines a grid cell index as row and column.
    /// </summary>
    public readonly struct CellIndex
    {
        #region Properties
        public int Row
        {
            get;
        }

        public int Column
        {
            get;
        }
        #endregion

        public CellIndex(int row, int column)
        {
            Row    = row;
            Column = column;
        }
    }

    /// <summary>
    /// Structure that defines element location as grid range 
    /// with beginning cell index and ending cell index.
    /// </summary>
    public readonly struct GraphicsElementLocation
    {
        #region Properties
        public CellIndex Begin
        {
            get;
        }

        public CellIndex End
        {
            get;
        }
        
        public bool IsUnique
        {
            get;
        }
        #endregion

        public GraphicsElementLocation(CellIndex begin, CellIndex end)
        {
            Begin    = begin;
            End      = end;
            IsUnique = Begin.Row == End.Row && Begin.Column == End.Column;
        }
    }

    /// <summary>
    /// Structure that defines graphics element as its geometry data
    /// and type hint. Elements do not have presentation data associated
    /// with them.
    ///
    /// This structure is mutable by design.
    /// </summary>
    public readonly struct GraphicsElement
    {
        #region Fields
        private readonly int hash;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the unique id of the element.
        /// </summary>
        public int Id
        {
            get;
        }

        /// <summary>
        /// Gets the type id of the element.
        /// Used for the actual rendering.
        /// </summary>
        public int TypeId
        {
            get;
        }
        #endregion

        public GraphicsElement(int id, int typeId)
        {
            Id     = id;
            TypeId = typeId;

            // Precompute hash for faster lookups.
            hash = HashUtils.Create()
                            .Append(Id)
                            .Append(typeId);
        }

        public override int GetHashCode()
            => hash;
    }

    /// <summary>
    /// Class that models a cell inside a graphics grid.
    /// </summary>
    public sealed class GraphicsElementCell : IEnumerable<int>
    {
        #region Fields
        private readonly HashSet<int> elements;
        #endregion

        public GraphicsElementCell()
            => elements = new HashSet<int>();

        public void Add(int elementId)
            => elements.Add(elementId);

        public void Remove(int elementId)
            => elements.Remove(elementId);

        public IEnumerator<int> GetEnumerator()
            => elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    /// <summary>
    /// Class representing spatial graphics layer that contains graphics
    /// elements and is responsible of partitioning them in correct way.
    /// </summary>
    public sealed class GraphicsElementLayer
    {
        #region Constant fields
        // Make all cells 10 units in size. For example if 1 unit is 32 pixels,
        // this makes each cell 320x320 pixels in size.
        public const float CellBounds = 10.0f;

        // Scale to allow computation of indices faster. For example to refer to index
        // with position (200, 300) we multiply this with scale and we get the index.
        public const float CellScale = 1.0f / CellBounds;

        // Initial rows count of the cell.
        private const int InitialRows = 8;

        // Initial columns count of the grid.
        private const int InitialColumns = 8;

        // Initial capacity of elements for the grid.
        private const int ElementsCapacity = 1024;
        #endregion

        #region Fields
        private readonly LinearGrowthArray<GraphicsElementLocation> locations;
        private readonly LinearGrowthArray<GraphicsElement>         elements;

        private GraphicsElementCell[][] grid;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the name of the layer.
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the order of the layer.
        /// </summary>
        public int Order
        {
            get;
        }

        public int Columns => grid[0].Length;

        public int Rows => grid.Length;

        public float Width => Columns * CellBounds;

        public float Height => Columns * CellBounds;

        /// <summary>
        /// Gets all elements contained in this layer.
        /// </summary>
        public IEnumerable<GraphicsElement> Elements
        {
            get
            {
                var results = new HashSet<GraphicsElement>();

                for (var i = 0; i < Rows; i++)
                {
                    for (var j = 0; j < Columns; j++)
                    {
                        foreach (var elementId in grid[i][j])
                            results.Add(elements.AtIndex(elementId));
                    }
                }

                return results;
            }
        }
        #endregion

        public GraphicsElementLayer(string name, int order)
        {
            Name  = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
            Order = order;

            elements  = new LinearGrowthArray<GraphicsElement>(ElementsCapacity);
            locations = new LinearGrowthArray<GraphicsElementLocation>(ElementsCapacity);

            // Create initial grid.
            grid = new GraphicsElementCell[InitialRows][];

            for (var i = 0; i < InitialRows; i++)
            {
                grid[i] = new GraphicsElementCell[InitialColumns];

                for (var j = 0; j < InitialColumns; j++)
                    grid[i][j] = new GraphicsElementCell();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClampAabb(ref Aabb aabb, out bool clamped)
        {
            var px = aabb.Left;
            var py = aabb.Top;

            clamped = px < 0.0f || py < 0.0f;

            if (!clamped)
                return;

            aabb = new Aabb(aabb, new Vector2(px < 0.0f ? aabb.HalfBounds.X : aabb.Position.X, py < 0.0f ? aabb.HalfBounds.Y : aabb.Position.Y));
        }

        private void AreaRange(in Aabb aabb, out GraphicsElementLocation range)
        {
            var beginColumn = (int)Math.Round((aabb.Left * CellScale));
            var endColumn   = (int)Math.Round((aabb.Right * CellScale));

            var beginRow = (int)Math.Round((aabb.Top * CellScale));
            var endRow   = (int)Math.Round((aabb.Bottom * CellScale));

            beginColumn = MathHelper.Clamp(beginColumn, 0, Columns - 1);
            endColumn   = MathHelper.Clamp(endColumn, 0, Columns - 1);

            beginRow = MathHelper.Clamp(beginRow, 0, Rows - 1);
            endRow   = MathHelper.Clamp(endRow, 0, Rows - 1);

            range = new GraphicsElementLocation(new CellIndex(beginRow, beginColumn), new CellIndex(endRow, endColumn));
        }

        private void GrowToFit(in Aabb aabb)
        {
            // Compute overlapping indices in delta and grow the grid by
            // that amount.
            var dco = Math.Round(Columns - aabb.Right * CellScale);
            var dro = Math.Round(Rows - aabb.Bottom * CellScale);

            // No need to grow the grid as the aabb does not overlap from 
            // right or bottom.
            if (dco > 0 && dro > 0)
                return;

            var dc = dco > 0 ? 0 : Math.Abs((int)dco) + 1;
            var dr = dro > 0 ? 0 : Math.Abs((int)dro) + 1;
            
            // Resize cells.
            var oldRows    = Rows;
            var oldColumns = Columns;

            if (dr != 0)
            {
                Array.Resize(ref grid, oldRows + dr);
            
                for (var i = oldRows; i < oldRows + dr; i++)
                {
                    // Create whole new cells starting from the new space 
                    // allocated.
                    grid[i] = new GraphicsElementCell[oldColumns + dr];
            
                    for (var j = 0; j < oldColumns + dc; j++)
                        grid[i][j] = new GraphicsElementCell();
                }   
            }
            
            // Resize old rows to have as much columns as the new columns.
            for (var i = 0; i < oldRows; i++)
            {
                Array.Resize(ref grid[i], oldColumns + dc);
                
                for (var j = oldColumns; j < oldColumns + dc; j++)
                    grid[i][j] = new GraphicsElementCell();
            }
        }

        /// <summary>
        /// Adds new element to the layer with given type id and initial aabb.
        /// </summary>
        public void Add(int elementId, int typeId, ref Aabb aabb, out bool clamped)
        {
            ClampAabb(ref aabb, out clamped);

            GrowToFit(aabb);

            // Compute location for the element.
            AreaRange(aabb, out var location);

            // Make sure we have enough space for this new element.
            if (elementId >= elements.Length) 
            {
                elements.Grow();
                locations.Grow();
            }
            
            // Store actual data of the element.
            locations.Insert(elementId, location);
            elements.Insert(elementId, new GraphicsElement(elementId, typeId));

            // Add to actual cells.
            if (location.IsUnique)
                grid[location.Begin.Row][location.Begin.Column].Add(elementId);
            else
            {
                // Multiple cells in range, add to them all. Use equal or smaller operator
                // because row or column can indicate single cell and location always points to
                // a index - it is zero based.
                for (var i = location.Begin.Row; i <= location.End.Row; i++)
                {
                    for (var j = location.Begin.Column; j <= location.End.Column; j++)
                        grid[i][j].Add(elementId);
                }
            }
        }

        /// <summary>
        /// Removes element with given id from the layer.
        /// </summary>
        public void Remove(int elementId)
        {
            ref var location = ref locations.AtIndex(elementId);

            // Remove to actual cells.
            if (location.IsUnique)
                grid[location.Begin.Row][location.Begin.Column].Remove(elementId);
            else
            {
                // Multiple cells in range, remove from them all.
                for (var i = location.Begin.Row; i <= location.End.Row; i++)
                {
                    for (var j = location.Begin.Column; j <= location.End.Column; j++)
                        grid[i][j].Remove(elementId);
                }
            }
        }

        /// <summary>
        /// Updates given element with given id with new aabb. 
        /// </summary>
        /// <param name="elementId">id of the element to be updated</param>
        /// <param name="aabb">new aabb of the element</param>
        /// <param name="clamped">boolean declaring was the element clamped, if true the aabb was changed</param>
        public void Update(int elementId, ref Aabb aabb, out bool clamped)
        {
            ref var location = ref locations.AtIndex(elementId);

            AreaRange(aabb, out var updateLocation);

            ClampAabb(ref aabb, out clamped);

            // If the element does not move enough to cause its cell range
            // to change, we can skip reinserting it to the grid.
            if (location.Begin.Column == updateLocation.Begin.Column &&
                location.Begin.Row == updateLocation.Begin.Row &&
                location.End.Column == updateLocation.End.Column &&
                location.End.Row == updateLocation.End.Row)
                return;

            // Reinsert, begin by removing.
            if (location.IsUnique)
                grid[location.Begin.Row][location.Begin.Column].Remove(elementId);
            else
            {
                for (var i = location.Begin.Row; i <= location.End.Row; i++)
                {
                    for (var j = location.Begin.Column; j <= location.End.Column; j++)
                        grid[i][j].Remove(elementId);
                }
            }

            // Reinsert, add to new locations.
            if (updateLocation.IsUnique)
                grid[updateLocation.Begin.Row][updateLocation.Begin.Column].Add(elementId);
            else
            {
                for (var i = updateLocation.Begin.Row; i <= updateLocation.End.Row; i++)
                {
                    for (var j = updateLocation.Begin.Column; j <= updateLocation.End.Column; j++)
                        grid[i][j].Add(elementId);
                }
            }

            // Record current location.
            location = updateLocation;
        }

        /// <summary>
        /// Queries the layer with given aabb. Puts elements found in
        /// the area to given results collection.
        /// </summary>
        public void QueryArea(in Aabb aabb, ISet<GraphicsElement> results)
        {
            AreaRange(aabb, out var range);

            if (range.IsUnique)
            {
                foreach (var elementId in grid[range.Begin.Row][range.Begin.Column])
                    results.Add(elements.AtIndex(elementId));
            }
            else
            {
                for (var i = range.Begin.Row; i <= range.End.Row; i++)
                {
                    for (var j = range.Begin.Column; j <= range.End.Column; j++)
                    {
                        foreach (var elementId in grid[i][j])
                            results.Add(elements.AtIndex(elementId));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Interface for implementing graphics layer systems. These systems
    /// are responsible of layer management.
    /// </summary>
    public interface IGraphicsLayerSystem : IObjectManagementSystem, IEnumerable<GraphicsElementLayer>
    {
        /// <summary>
        /// Creates new layer with given name and order and
        /// returns it to the caller.
        /// </summary>
        GraphicsElementLayer Create(string name, int order);

        /// <summary>
        /// Deletes layer with given name.
        /// </summary>
        void Delete(GraphicsElementLayer layer);

        /// <summary>
        /// Attempts to get layer with given name. Returns boolean declaring whether the layer was found.
        /// </summary>
        bool TryGetLayer(string name, out GraphicsElementLayer layer);
    }

    /// <summary>
    /// Default implementation of <see cref="IGraphicsLayerSystem"/>.
    /// </summary>
    public class GraphicsLayerSystem : GameEngineSystem, IGraphicsLayerSystem
    {
        #region Fields
        private readonly List<GraphicsElementLayer> layers;
        #endregion

        [BindingConstructor]
        public GraphicsLayerSystem()
            => layers = new List<GraphicsElementLayer>();

        public GraphicsElementLayer Create(string name, int order)
        {
            var layer = new GraphicsElementLayer(name, order);

            layers.Add(layer);

            if (layers.Count > 1)
            {
                layers.Sort((x, y) =>
                {
                    var xo = x.Order;
                    var yo = y.Order;

                    if (xo < yo)
                        return -1;

                    return xo > yo ? 1 : 0;
                });
            }

            return layer;
        }

        public void Delete(GraphicsElementLayer layer)
        {
            if (layer == null)
                throw new ArgumentNullException(nameof(layer));

            if (!layers.Remove(layer))
                throw new InvalidOperationException($"could not delete layer {layer.Name}");
        }

        public bool TryGetLayer(string name, out GraphicsElementLayer layer)
        {
            layer = layers.FirstOrDefault(l => l.Name == name);

            return layer != null;
        }

        public void Clear()
        {
            while (layers.Count != 0)
                Delete(layers[0]);
        }

        public IEnumerator<GraphicsElementLayer> GetEnumerator()
            => layers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    /// <summary>
    /// Phase that handles drawing layers based on ECS components.
    /// </summary>
    public sealed class GraphicsLayerPipelinePhase : GraphicsPipelinePhase
    {
        #region Fields
        private readonly IGraphicsLayerSystem layers;

        private readonly IViewSystem views;

        private readonly IGraphicsComponentSystem[] lookup;
        private readonly HashSet<GraphicsElement>   results;
        #endregion

        public GraphicsLayerPipelinePhase(IGameHost host,
                                          IGraphicsPipelineSystem pipelines,
                                          IGraphicsLayerSystem layers,
                                          IViewSystem views,
                                          IEnumerable<IGraphicsComponentSystem> systems,
                                          int index)
            : base(host, pipelines, index, new GraphicsFragmentSettings(
                       SpriteSortMode.Deferred,
                       BlendState.AlphaBlend,
                       SamplerState.PointClamp,
                       DepthStencilState.None,
                       RasterizerState.CullNone))
        {
            this.views  = views ?? throw new ArgumentNullException(nameof(views));
            this.layers = layers ?? throw new ArgumentNullException(nameof(layers));

            // Create component lookup for rendering elements.
            lookup = new IGraphicsComponentSystem[systems.Max(s => s.GraphicsComponentTypeId) + 1];

            foreach (var system in systems)
                lookup[system.GraphicsComponentTypeId] = system;

            // Reuse results hashset to ease GC pressure.
            results = new HashSet<GraphicsElement>(1024);
        }

        public override void Execute(IGameEngineTime time)
        {
            var fragment = Pipeline.FragmentAtIndex(Index);

            foreach (var view in views)
            {
                fragment.Begin(view);

                foreach (var layer in layers)
                {
                    layer.QueryArea(view.BoundingBox, results);

                    foreach (var element in results)
                        lookup[element.TypeId].DrawElement(element.Id, fragment);
                }

                fragment.End();
            }

            results.Clear();
        }
    }
}