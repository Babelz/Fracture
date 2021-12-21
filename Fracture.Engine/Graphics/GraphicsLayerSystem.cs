using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fracture.Common.Collections;
using Fracture.Common.Di.Attributes;
using Fracture.Common.Util;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Ecs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
      #endregion

      public GraphicsElementLocation(CellIndex begin, CellIndex end)
      {
         Begin = begin;
         End   = end;
      }
      
      /// <summary>
      /// Returns boolean whether this location occupies
      /// single unique location.
      /// </summary>
      public bool Unique() 
         => Begin.Row    == End.Row &&
            Begin.Column == End.Column;
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
      
      public void Add(int id)
         => elements.Add(id);
      
      public void Remove(int id)
         => elements.Remove(id);
      
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
      private readonly LinearGrowthArray<GraphicsElement> elements;
      
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
      
      public int Columns 
         => grid[0].Length;
      
      public int Rows 
         => grid.Length;
      
      public float Width 
         => Columns * CellBounds;
      
      public float Height 
         => Columns * CellBounds;
      
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
                  foreach (var id in grid[i][j]) 
                     results.Add(elements.AtIndex(id));
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
      
      private void AreaRange(out GraphicsElementLocation range)
      {
         var beginColumn = (int)(Columns * CellBounds * CellScale);
         var endColumn   = (int)(Columns * CellBounds * CellScale);

         var beginRow = (int)(Rows * CellBounds * CellScale);
         var endRow   = (int)(Rows * CellBounds * CellScale);
         
         beginColumn = MathHelper.Clamp(beginColumn, 0, grid[0].Length - 1);
         endColumn = MathHelper.Clamp(endColumn, 0, grid[0].Length - 1);
         
         beginRow = MathHelper.Clamp(beginRow, 0, grid.Length - 1);
         endRow   = MathHelper.Clamp(endRow, 0, grid.Length - 1);

         range = new GraphicsElementLocation(new CellIndex(beginRow, beginColumn), new CellIndex(endRow, endColumn));
      }
      
      private void GrowToFit(in Aabb aabb)
      {
         // Compute overlapping indices in delta and grow the grid by
         // that amount.
         var dc = Columns - (int)(aabb.Right * CellScale);
         var dr = Rows - (int)(aabb.Bottom * CellScale);
         
         // No need to grow the grid as the aabb does not overlap from 
         // right or bottom.
         if (dc >= 0 || dr >= 0) 
            return;
         
         dc = Math.Abs(dc) + 1;
         dr = Math.Abs(dr) + 1;
         
         // Resize cells.
         var oldRows    = Rows;
         var oldColumns = Columns;
         
         Array.Resize(ref grid, oldRows + dr);
         
         for (var i = oldRows; i < oldRows + dr; i++)
         {
            // Create whole new cells starting from the new space 
            // allocated.
            grid[i] = new GraphicsElementCell[oldColumns + dr];
            
            for (var j = 0; j < oldColumns + dc; j++) 
               grid[i][j] = new GraphicsElementCell();
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
      public void Add(int id, int typeId, ref Aabb aabb, out bool clamped)
      {
         ClampAabb(ref aabb, out clamped);
         
         GrowToFit(aabb);
         
         // Make sure we have enough space for this new element.
         if (id >= elements.Length)
            elements.Grow();
         
         // Compute location for the element.
         AreaRange(out var location);

         // Store actual data of the element.
         locations.Insert(id, location);
         elements.Insert(id, new GraphicsElement(id, typeId));
         
         // Add to actual cells.
         if (location.Unique())
            grid[location.Begin.Row][location.Begin.Column].Add(id);
         else
         {
            // Multiple cells in range, add to them all. Use equal or smaller operator
            // because row or column can indicate single cell and location always points to
            // a index - it is zero based.
            for (var i = location.Begin.Row; i <= location.End.Row; i++)
            {
               for (var j = location.Begin.Column; j <= location.End.Column; j++)
                  grid[i][j].Add(id);
            }
         }
      }
      
      /// <summary>
      /// Removes element with given id from the layer.
      /// </summary>
      public void Remove(int id)
      {
         ref var location = ref locations.AtIndex(id);
         
         // Remove to actual cells.
         if (location.Unique())
            grid[location.Begin.Row][location.Begin.Column].Remove(id);
         else
         {
            // Multiple cells in range, remove from them all.
            for (var i = location.Begin.Row; i <= location.End.Row; i++)
            {
               for (var j = location.Begin.Column; j <= location.End.Column; j++)
                  grid[i][j].Remove(id);
            }
         }
      }
      
      /// <summary>
      /// Updates given element with given id with new aabb. 
      /// </summary>
      /// <param name="id">id of the element to be updated</param>
      /// <param name="aabb">new aabb of the element</param>
      /// <param name="clamped">boolean declaring was the element clamped, if true the aabb was changed</param>
      public void Update(int id, ref Aabb aabb, out bool clamped)
      {
         ref var location = ref locations.AtIndex(id);
         
         AreaRange(out var updateLocation);
         
         ClampAabb(ref aabb, out clamped);
         
         // If the element does not move enough to cause its cell range
         // to change, we can skip reinserting it to the grid.
         if (location.Begin.Column == updateLocation.Begin.Column &&
             location.Begin.Row == updateLocation.Begin.Row &&
             location.End.Column == updateLocation.End.Column &&
             location.End.Row == updateLocation.End.Row)
            return;
         
         // Reinsert, begin by removing.
         if (location.Unique())
            grid[location.Begin.Row][location.Begin.Column].Remove(id);
         else
         {
            for (var i = location.Begin.Row; i <= location.End.Row; i++)
            {
               for (var j = location.Begin.Column; j <= location.End.Column; j++)
                  grid[i][j].Remove(id);
            }  
         }

         // Reinsert, add to new locations.
         if (updateLocation.Unique())
            grid[updateLocation.Begin.Row][updateLocation.Begin.Column].Add(id);
         else
         {
            for (var i = updateLocation.Begin.Row; i <= updateLocation.End.Row; i++)
            {
               for (var j = updateLocation.Begin.Column; j <= updateLocation.End.Column; j++)
                  grid[i][j].Add(id);
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
         AreaRange(out var range);
         
         if (range.Unique())
         {
            foreach (var id in grid[range.Begin.Row][range.Begin.Column]) 
               results.Add(elements.AtIndex(id));
         }
         else
         {
            for (var i = range.Begin.Row; i <= range.End.Row; i++)
            {
               for (var j = range.Begin.Column; j <= range.End.Column; j++)
               {
                  foreach (var id in grid[i][j]) 
                     results.Add(elements.AtIndex(id));
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
      {
         layers = new List<GraphicsElementLayer>();
      }
      
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
      private readonly HashSet<GraphicsElement> results;
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