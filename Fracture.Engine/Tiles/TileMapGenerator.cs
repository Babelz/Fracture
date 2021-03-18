using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Class that contains seed and random number generators
   /// for the generator.
   /// </summary>
   public sealed class TileMapGeneratorSeed
   {
      #region Fields
      private readonly Random random;
      #endregion
      
      #region Properties
      /// <summary>
      /// Gets the seed used for this generation.
      /// </summary>
      public int Seed
      {
         get;
      }
      #endregion

      public TileMapGeneratorSeed(int seed)
      {
         Seed = seed;
         
         random = new Random(Seed);   
      }
      
      /// <summary>
      /// Gets next random int between min and max.
      /// </summary>
      public int NextInt(int min, int max)
         => random.Next(min, max);
    
      /// <summary>
      /// Gets next random in between int min and int max.
      /// </summary>
      public int NextInt()
         => random.Next();
      
      /// <summary>
      /// Gets next random float between 0.0f and 1.0f.
      /// </summary>
      public float NextFloat()
         => (float)random.NextDouble();
   }
   
   /// <summary>
   /// Class that contains working context for the tile map generator and
   /// all it's sub generators.
   /// </summary>
   public class TileMapGeneratorContext
   {
      #region Properties
      public IGameEngine Engine
      {
         get;
      }

      public TileMap Map
      {
         get;
      }
      
      public TileMapGeneratorSeed Seed
      {
         get;
      }
      #endregion

      public TileMapGeneratorContext(IGameEngine engine, 
                                     TileMap map, 
                                     TileMapGeneratorSeed seed)
      {
         Engine = engine ?? throw new ArgumentNullException(nameof(engine));
         Map    = map ?? throw new ArgumentNullException(nameof(map));
         Seed   = seed ?? throw new ArgumentNullException(nameof(seed));
      }
   }

   /// <summary>
   /// Base class for generating different tile maps. Provides
   /// basic surface generation and nothing else. Extend this
   /// class to modify the generation.
   /// </summary>
   public abstract class TileMapGenerator
   {
      protected TileMapGenerator()
      {
      }
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      protected static void GenerateBehaviours(TileMapGeneratorContext context)
      {
         foreach (var tileType in context.Map.Where(t => !t.IsEmpty()).GroupBy(t => t.Type).Select(g => g.First().Type))
            context.Map.Behaviours.Register(tileType, TileBehaviourFactory.Create(context.Engine, tileType));
      }
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      protected static void GeneratePainters(TileMapGeneratorContext context)
      {
         foreach (var tileType in context.Map.Where(t => !t.IsEmpty()).GroupBy(t => t.Type).Select(g => g.First().Type))
            context.Map.Painters.Register(tileType, TilePainterFactory.Create(context.Engine, tileType));
      }
      
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      protected static void GenerateAttributes(TileMapGeneratorContext context)
      {
         foreach (var tileType in context.Map.Where(t => !t.IsEmpty()).GroupBy(t => t.Type).Select(g => g.First().Type))
            context.Map.Attributes.Register(tileType, TileAttributesFactory.Create(context.Engine, tileType));
      }

      /// <summary>
      /// Generates surface for the map. Override in inheriting
      /// class to do custom generation.
      /// </summary>
      public virtual void Generate(TileMapGeneratorContext context)
      {
      }
   }
}