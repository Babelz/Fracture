using Microsoft.Xna.Framework;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Interface for implementing generic tile generators. These generators
   /// are used for basic shaping of tile maps geometry and characteristics.
   /// </summary>
   public interface ITileGenerator
   {
      void Generate(TileMapGeneratorContext context);
   }
   
   /// <summary>
   /// Interface for implementing room generators. Rooms can be anything
   /// from dungeons to biomes. Rooms with different assets can share
   /// logic for generating them.
   /// </summary>
   public interface ITileRoomGenerator
   {
      void Generate(TileMapGeneratorContext context, in Rectangle area);
   }
   
   /// <summary>
   /// Generator that generates flat surface to the map beginning
   /// from the center of the map in vertical direction.
   /// </summary>
   public sealed class FlatMapTileGenerator : ITileGenerator
   {
      #region Fields
      private readonly ushort dirt;
      private readonly ushort floor;
      #endregion
      
      /// <summary>
      /// Creates new instance of <see cref="FlatMapTileGenerator"/> that generates
      /// flat tile map with floor and dirt layers underneath the surface.
      /// </summary>
      /// <param name="dirt">tile type of dirt</param>
      /// <param name="floor">tile type of floor</param>
      public FlatMapTileGenerator(ushort dirt, ushort floor)
      {
         this.dirt  = dirt;
         this.floor = floor;
      }
      
      public void Generate(TileMapGeneratorContext context)
      {
         var map     = context.Map;
         var surface = map.TileEngine.MapSurface;
         var width   = map.TileEngine.MapSize.X;
         
         
      }
   }
}