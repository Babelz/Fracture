using System;
using System.IO;
using System.Linq;

namespace Fracture.Engine.Tiles.Themes
{
   [TileMapTheme("Terra")]
   public static class Terra
   {
      #region Constant fields
      private const string ContentRoot = "content//maps//terra";
      #endregion
      
      /// <summary>
      /// Enumeration containing know tile types for the terra theme.
      /// </summary>
      public enum TileType : ushort
      {
         Dirt      = 1,
         DirtFloor = 2
      }
      
      [TileMapThemeLoad()]
      public static void Load()
      {     
         TilePainterFactory.RegisterActivator((ushort)TileType.Dirt, (engine) => new TileTexturePainter(engine, Path.Combine(ContentRoot, "textures", "dirt")));
         TilePainterFactory.RegisterActivator((ushort)TileType.DirtFloor, (engine) => new TileTexturePainter(engine, Path.Combine(ContentRoot, "textures", "dirt-floor")));
         
         TileFactory.RegisterDefaultActivators(Enum.GetValues(typeof(TileType)).Cast<ushort>());
      }
   }

   public sealed class TerraTileMapGenerator : TileMapGenerator
   {
      #region Fields
      private readonly ITileGenerator surfaceGenerator;
      #endregion
      
      public TerraTileMapGenerator(ITileGenerator surfaceGenerator)
      {
         this.surfaceGenerator = surfaceGenerator ?? throw new ArgumentNullException(nameof(surfaceGenerator));
      }

      public override void Generate(TileMapGeneratorContext context)
      {
         surfaceGenerator.Generate(context);
         
         GeneratePainters(context);
         GenerateBehaviours(context);
         GenerateAttributes(context);
      }
   }
}