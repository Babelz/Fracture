using System;
using System.Collections.Generic;
using Fracture.Engine.Core;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Interface for declaring tile behaviour components. These allow
   /// tiles to contain custom logic when tile map is updated. 
   /// </summary>
   public interface ITileBehaviour
   {
      void Update(in Tile tile, int row, int column, TileMap map, IGameEngineTime time);
   }
   
   public delegate ITileBehaviour TileBehaviourFactoryDelegate(IGameEngine engine);

   public static class TileBehaviourFactory 
   {
      #region Static fields
      private static readonly Dictionary<ushort, TileBehaviourFactoryDelegate> Lookup =
         new Dictionary<ushort, TileBehaviourFactoryDelegate>();
      #endregion
      
      public static void RegisterActivator(ushort type, TileBehaviourFactoryDelegate factory)
      {
         if (Lookup.ContainsKey(type))
            throw new InvalidOperationException($"activator {type} already exists");
         
         Lookup.Add(type, factory);
      }
      
      public static ITileBehaviour Create(IGameEngine engine, ushort type)
         => Lookup[type](engine);
   }
}