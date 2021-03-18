using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fracture.Common.Collections;
using Fracture.Engine.Core;
using Fracture.Engine.Core.Primitives;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Class that contains tile map geometry data.
   /// </summary>
   public sealed class TileEngine
   {
      #region Properties
      /// <summary>
      /// Gets the tile size in world units.
      /// </summary>
      public Vector2 TileBounds
      {
         get;
      }
      
      /// <summary>
      /// Gets tile size in screen units.
      /// </summary>
      public Point TileSize
      {
         get;
      }

      /// <summary>
      /// Gets the map size in tiles.
      /// </summary>
      public Point MapSize
      {
         get;
      }
      
      /// <summary>
      /// Gets map bounds in world units.
      /// </summary>
      public Vector2 MapBounds
      {
         get;
      }
      
      /// <summary>
      /// Gets the surface coordinate in y-axis. 
      /// </summary>
      public int MapSurface
      {
         get;
      }
      #endregion

      public TileEngine(Point tileSize, Point mapSize, int mapSurface)
      {
         if (tileSize.X < 0.0f) 
            throw new ArgumentOutOfRangeException(nameof(tileSize.X));
         
         if (tileSize.Y < 0.0f) 
            throw new ArgumentOutOfRangeException(nameof(tileSize.Y));

         if (mapSize.X < 0.0f) 
            throw new ArgumentOutOfRangeException(nameof(mapSize.X));
         
         if (mapSize.Y < 0.0f) 
            throw new ArgumentOutOfRangeException(nameof(mapSize.Y));

         if (mapSurface < 0 || mapSurface >= mapSize.Y) 
            throw new ArgumentOutOfRangeException(nameof(mapSurface), $"must be greater than 0 and less than {nameof(mapSize.Y)}");
         
         TileSize   = tileSize;
         MapSize    = mapSize;
         MapSurface = mapSurface;
         
         TileBounds = Transform.ToWorldUnits(tileSize.X, tileSize.Y);
         MapBounds  = Transform.ToWorldUnits(new Vector2(mapSize.X * tileSize.X, mapSize.Y * tileSize.Y));
      }
      
      /// <summary>
      /// Translates given world position to map indices.
      /// </summary>
      public Point TranslatePosition(in Vector2 position)
         => new Point(TranslateColumn(position.X), TranslateRow(position.Y));
      
      /// <summary>
      /// Translates given world position value to row value in
      /// the map.
      /// </summary>
      public int TranslateRow(float world)
         => MathHelper.Clamp((int)Math.Floor(world / TileBounds.Y), 0, MapSize.Y);
   
      /// <summary>
      /// Translates given world position value to column value
      /// in the map.
      /// </summary>
      /// <param name="world"></param>
      /// <returns></returns>
      public int TranslateColumn(float world)
         => MathHelper.Clamp((int)Math.Floor(world / TileBounds.Y), 0, MapSize.Y);
   }

   /// <summary>
   /// Tile maps contain tiles and their transform and size are defined by the
   /// tile engine provided. Tile maps can't be resized once they are created.
   /// </summary>
   public sealed class TileMap : IEnumerable<Tile>
   {
      #region Fields
      private readonly Tile[][] tiles;
      #endregion
      
      #region Properties
      public TileEngine TileEngine
      {
         get;
      }

      public TileMapMapChunkContainer Chunks
      {
         get;
      }
      
      public LinearRegistry<ITilePainter> Painters
      {
         get;
      }
      public LinearRegistry<ITileBehaviour> Behaviours
      {
         get;
      }
      public LinearRegistry<ITileAttributes> Attributes
      {
         get;
      }
      #endregion

      public TileMap(TileEngine tileEngine)
      {
         TileEngine = tileEngine ?? throw new ArgumentNullException(nameof(tileEngine));
         
         tiles = new Tile[TileEngine.MapSize.Y][];
         
         for (var i = 0; i < TileEngine.MapSize.Y; i++)
            tiles[i] = new Tile[TileEngine.MapSize.X];
         
         Chunks = new TileMapMapChunkContainer(tileEngine);
         
         Painters   = new LinearRegistry<ITilePainter>();
         Behaviours = new LinearRegistry<ITileBehaviour>();
         Attributes = new LinearRegistry<ITileAttributes>();
      }
      
      public ref Tile TileAtIndex(int row, int column)
         => ref tiles[row][column];
      
      public void InsertTile(int row, int column, in Tile tile)
         => tiles[row][column] = tile;

      public IEnumerator<Tile> GetEnumerator()
      {
         for (var i = 0; i < TileEngine.MapSize.Y; i++)
            for (var j = 0; j < TileEngine.MapSize.X; j++)
               yield return tiles[i][j];
      }

      IEnumerator IEnumerable.GetEnumerator()
         => GetEnumerator();

      public void DumpTiles(string path)
      {
         using var file = File.Open(path, FileMode.CreateNew | FileMode.Truncate);
         
         var sb = new StringBuilder();
         
         for (var i = 0; i < TileEngine.MapSize.Y; i++)
         {
            sb.Append("[ ");
            
            for (var j = 0; j < TileEngine.MapSize.X - 1; j++)
            {
               sb.Append(tiles[i][j].Type);
               sb.Append(" ");
            }
            
            sb.Append(tiles[i][TileEngine.MapSize.X - 1].Type);
            sb.Append(" ]\n");
         }
         
         var data = Encoding.UTF8.GetBytes(sb.ToString());
         
         file.Write(data, 0, data.Length);
      }
   }
   
   /// <summary>
   /// Interface for implementing prioritization logic for tile
   /// map chunk updates. 
   /// </summary>
   public interface ITileMapChunkPrioritizer
   {
      /// <summary>
      /// Prioritizes chunk that contains given world position
      /// in tile map space to be updated during next update.
      /// </summary>
      void Prioritize(Vector2 position);
   }
   
   /// <summary>
   /// System responsible of tile map management and actual presentation
   /// and underlying logic for the tile map.
   /// </summary>
   public interface ITileMapSystem : IActiveGameEngineSystem, IObjectManagementSystem
   {
      #region Properties
      /// <summary>
      /// Gets the active map currently in use by the system.
      /// </summary>
      TileMap Map
      {
         get;
      }
      #endregion

      /// <summary>
      /// Creates new tile map with given geometry
      /// parameters.
      /// </summary>
      TileMap Create(TileEngine tileEngine);
      
      /// <summary>
      /// Deletes given map.
      /// </summary>
      void Delete(TileMap map);
      
      /// <summary>
      /// Activates given map.
      /// </summary>
      void Activate(TileMap map);
      
      /// <summary>
      /// Deactivates current map.
      /// </summary>
      void Deactivate();
   }
   
   public class TileMapPipelinePhase : GraphicsPipelinePhase
   {
      #region Fields
      private readonly ITileMapSystem maps;
      
      private readonly IViewSystem views;
      #endregion

      public TileMapPipelinePhase(IGameEngine engine, int index) 
         : base(engine, index, new GraphicsFragmentSettings(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone))
      {
         maps  = engine.Systems.First<ITileMapSystem>();
         views = engine.Systems.First<IViewSystem>();
      }
      
      public override void Execute(IGameEngineTime time)
      {
         if (maps.Map == null)
            return;
         
         var fragment = Pipeline.FragmentAtIndex(Index);
      
         foreach (var view in views)
         {
            fragment.Begin(view);
            
            var bounds  = view.BoundingBox;
            var map     = maps.Map;
            var mapSize = map.TileEngine.MapSize;
            
            var beginRow = MathHelper.Clamp(
               maps.Map.TileEngine.TranslateRow(Transform.ToWorldUnits(bounds.Top)),
               0,
               mapSize.Y
            );
            
            var endRow = MathHelper.Clamp(
               maps.Map.TileEngine.TranslateRow(Transform.ToWorldUnits(bounds.Bottom)) + 2,
               0,
               mapSize.Y
            );
            
            var beginColumn = MathHelper.Clamp(
               maps.Map.TileEngine.TranslateColumn(Transform.ToWorldUnits(bounds.Left)),
               0,
               mapSize.X
            );
               
            var endColumn = MathHelper.Clamp(
               maps.Map.TileEngine.TranslateColumn(Transform.ToWorldUnits(bounds.Right)) + 2, 
               0,
               mapSize.X
            );

            ushort lastTileType          = 0;
            ITilePainter lastTilePainter = null;

            for (var row = beginRow; row < endRow; row++)
            {
               for (var column = beginColumn; column < endColumn; column++)
               {
                  ref var current = ref map.TileAtIndex(row, column);

                  if (current.IsEmpty())
                     continue;
                  
                  if (current.Type != lastTileType)
                  {
                     lastTileType    = current.Type;
                     lastTilePainter = map.Painters.AtLocation(current.Type);
                  }
                  
                  if (lastTilePainter == null) 
                     throw new InvalidOperationException($"tile painter for tile type {lastTileType} was not found");
                  
                  lastTilePainter.Draw(current, row, column, map, fragment, time);
               }
            }
            
            fragment.End();
         }
      }
   }
   
   /// <summary>
   /// Structure presenting single chunk inside the map.
   /// </summary>
   public readonly struct TileMapChunk 
   {
      #region Fields
      // Precomputed area for the chunk.
      private readonly int area;
      #endregion
      
      #region Properties
      /// <summary>
      /// Gets the chunk bounds in tile map space.
      /// </summary>
      public Rectangle Bounds
      {
         get;
      }
      #endregion

      public TileMapChunk(int x, int y, int width, int height)
      {
         if (y < 0) 
            throw new ArgumentOutOfRangeException(nameof(y));
         
         if (x < 0) 
            throw new ArgumentOutOfRangeException(nameof(x));
         
         if (width < 0) 
            throw new ArgumentOutOfRangeException(nameof(width));
         
         if (height < 0) 
            throw new ArgumentOutOfRangeException(nameof(height));
         
         Bounds = new Rectangle(x, y, width, height);
         
         area = width * height;
      }
      
      /// <summary>
      /// Returns boolean declaring whether this chunk contains
      /// given point.
      /// </summary>
      public bool ContainsPoint(in Point point)
      {
         // Do area based check, precompute area widths
         // and heights from point to sides.
         var rw = point.X - Bounds.Right;
         var lw = Bounds.Left - point.X;
         var th = Bounds.Top - point.Y;
         var bh = Bounds.Bottom - point.Y;
         
         // Compute areas from top vertices to point.
         var tla = Math.Abs(lw * th);
         var tra = Math.Abs(rw * th);
         
         // Compute areas from bottom vertices to point.
         var bla = Math.Abs(lw * bh);
         var bra = Math.Abs(rw * bh);
         
         return (tla + tra + bla + bra) == area;
      }
   }
   
   public sealed class TileMapMapChunkContainer : ITileMapChunkPrioritizer, IEnumerable<TileMapChunk>
   {
      #region Constant fields
      /// <summary>
      /// Bounds of a chunk, width and height.
      /// </summary>
      public const int ChunkBounds = 128;
      #endregion

      #region Fields
      private readonly TileEngine tileEngine;
      
      private readonly TileMapChunk[] chunks;
      
      private readonly Queue<int> queue;
      
      private readonly HashSet<int> prioritized;
      #endregion

      public TileMapMapChunkContainer(TileEngine tileEngine)
      {
         this.tileEngine = tileEngine;
         
         // Generate chunks.
         var chunkRows    = tileEngine.MapSize.Y / ChunkBounds;
         var chunkColumns = tileEngine.MapSize.X / ChunkBounds;
         
         var chunkWidthMod  = tileEngine.MapSize.X % ChunkBounds;
         var chunkHeightMod = tileEngine.MapSize.Y % ChunkBounds;
         
         if (chunkRows == 0 && chunkColumns == 0)
         {
            // Single chunk map.
            chunks = new[] { new TileMapChunk(0, 0, chunkWidthMod, chunkHeightMod) };
            queue  = new Queue<int>(new[] { 0 });
         }
         else
         {
            // Multiple chunk map.
            chunks = new TileMapChunk[chunkRows * chunkColumns];
            queue  = new Queue<int>(Enumerable.Range(0, chunkRows * chunkColumns));

            // Generate all chunks but not the last ones. Take size
            // modulo in to account and make last rows and columns wider by
            // the modulo amount.
            for (var row = 0; row < chunkRows; row++)
            {
               // Take height modulo in to account with last rows.
               var height = ChunkBounds;
               var y      = row * ChunkBounds;
            
               if (row + 1 >= chunkRows && chunkHeightMod != 0) 
                  height += chunkHeightMod;

               for (var column = 0; column < chunkColumns; column++)
               {
                  // Take width modulo in to account with last columns. 
                  var width = ChunkBounds;
                  var x     = column * ChunkBounds;
               
                  if (column + 1 >= chunkColumns && chunkWidthMod != 0) 
                     width += chunkWidthMod;

                  chunks[(row * chunkColumns) + column] = new TileMapChunk(
                     x,
                     y,
                     width,
                     height
                  );
               }
            }  
         }

         // Create queues.
         prioritized = new HashSet<int>();
      }
      
      public void Prioritize(Vector2 position)
      {
         for (var i = 0; i < chunks.Length; i++)
         {
            ref var chunk = ref chunks[i];

            if (!chunk.ContainsPoint(tileEngine.TranslatePosition(position))) 
               continue;
            
            prioritized.Add(i);
            
            return;
         }
         
         throw new InvalidOperationException($"could not prioritize position {position}");
      }
      
      /// <summary>
      /// Returns prioritized chunks for updates.
      /// </summary>
      public IEnumerable<TileMapChunk> Prioritized()
      {
         // How many chunks per frame are being updated.
         const int ChunkUpdatesPerFrame = 8;

         // Prioritize next chunks for updates.
         var count = 0;
         
         while (count < ChunkUpdatesPerFrame)
         {
            var chunk = queue.Dequeue();
            
            if (prioritized.Add(chunk))
               count++;
            
            queue.Enqueue(chunk);
            
            // Break quick if we can't get more chunks.
            if (prioritized.Count == chunks.Length)
               break;
         }
         
         // Yield prioritized chunks and clear queue.
         foreach (var index in prioritized) 
            yield return chunks[index];
         
         prioritized.Clear();
      }

      public IEnumerator<TileMapChunk> GetEnumerator()
         => ((IEnumerable<TileMapChunk>)chunks).GetEnumerator();
      
      IEnumerator IEnumerable.GetEnumerator()
         => chunks.GetEnumerator();
   }

   public sealed class TileMapSystem : ActiveGameEngineSystem, ITileMapSystem
   {
      #region Fields
      private readonly List<TileMap> maps;
      #endregion

      #region Properties
      public TileMap Map
      {
         get;
         private set;
      }
      #endregion
      
      /// <summary>
      /// Creates new instance of tile map system.
      /// </summary>
      public TileMapSystem(int priority) 
         : base(priority)
      {
         maps = new List<TileMap>();
         
         TileMapThemeLoader.Load();
      }
      
      public TileMap Create(TileEngine tileEngine)
      {
         var map = new TileMap(tileEngine);
         
         maps.Add(map);

         return map;
      }

      public void Delete(TileMap map)
      {
         if (!maps.Remove(map)) 
            throw new InvalidOperationException("map does not exist in the system");
         
         if (ReferenceEquals(map, Map))
            Deactivate();
      }

      public void Activate(TileMap map)
      {
         if (Map != null)
            Deactivate();
         
         Map = map;
      }

      public void Deactivate()
      {
         if (Map == null)
            throw new InvalidOperationException("no map is active");
         
         Map = null;
      }

      public override void Update(IGameEngineTime time)
      {
         if (Map == null)
            return;
         
         // Get prioritized chunks and run map updates.
         foreach (var chunk in Map.Chunks.Prioritized())
         {            
            for (var row = chunk.Bounds.Top; row < chunk.Bounds.Bottom; row++)
            {
               ITileBehaviour behaviour = null;
               ushort lastTileType      = 0;
               
               for (var column = chunk.Bounds.Left; column < chunk.Bounds.Right; column++)
               {
                  // Go trough all tiles in prioritized chunks and update them,
                  // skip all empty tiles.
                  ref var tile = ref Map.TileAtIndex(row, column);
                  
                  if (tile.IsEmpty())
                     continue;
                  
                  // Change behaviour if the tile type changes.
                  if (tile.Type != lastTileType)
                  {
                     lastTileType = tile.Type;
                     behaviour    = Map.Behaviours.AtLocation(tile.Type);
                  }

                  // Allow behaviour to run updates for tiles.
                  behaviour?.Update(tile, row, column, Map, time);
               }
            }
         }
      }

      public void Clear()
      {
         while (maps.Count != 0) 
            Delete(maps[0]);
      }
   }
}