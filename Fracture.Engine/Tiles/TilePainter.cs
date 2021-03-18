using System;
using System.Collections.Generic;
using Fracture.Engine.Core;
using Fracture.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Interface for declaring tile drawing components. These allow
   /// tiles to contain custom presentation logic then drawing the map.
   /// </summary>
   public interface ITilePainter
   {
      void Draw(in Tile tile, int row, int column, TileMap map, IGraphicsFragment fragment, IGameEngineTime time); 
   }

   public delegate ITilePainter TilePainterFactoryDelegate(IGameEngine engine);

   public static class TilePainterFactory 
   {
      #region Static fields
      private static readonly Dictionary<ushort, TilePainterFactoryDelegate> Lookup =
         new Dictionary<ushort, TilePainterFactoryDelegate>();
      #endregion
      
      public static void RegisterActivator(ushort type, TilePainterFactoryDelegate factory)
      {
         if (Lookup.ContainsKey(type))
            throw new InvalidOperationException($"activator {type} already exists");
         
         Lookup.Add(type, factory);
      }
      
      public static ITilePainter Create(IGameEngine engine, ushort type)
         => Lookup[type](engine);
   }
   
   /// <summary>
   /// Simple tile painter for painting single textures.
   /// </summary>
   public sealed class TileTexturePainter : ITilePainter
   {
      #region Fields
      private readonly Texture2D texture;
      #endregion
      
      public TileTexturePainter(IGameEngine engine, string texturePath)
         => texture = engine.Services.First<ContentManager>().Load<Texture2D>(texturePath);
      
      public void Draw(in Tile tile, int row, int column, TileMap map, IGraphicsFragment fragment, IGameEngineTime time)
      {
         fragment.DrawSprite(
            new Vector2(column * map.TileEngine.TileSize.X, row * map.TileEngine.TileSize.Y),
            Vector2.One,
            0.0f,
            new Vector2(map.TileEngine.TileSize.X * 0.5f, map.TileEngine.TileSize.Y * 0.5f),
            new Vector2(map.TileEngine.TileSize.X, map.TileEngine.TileSize.Y),
            texture,
            Color.White
         );
      }
   }
}