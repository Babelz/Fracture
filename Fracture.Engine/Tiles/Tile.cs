using System;
using System.Collections.Generic;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Exception throw in case tile type is undefined at point
   /// where it should not be.
   /// </summary>
   public sealed class TileUndefinedException : Exception
   {
      #region Properties
      /// <summary>
      /// Gets the tile type of the undefined tile.
      /// </summary>
      public ushort TileType
      {
         get;
      }

      /// <summary>
      /// Gets the optional tile type enum where
      /// the tile type should have been defined.
      /// </summary>
      public Type TileTypeEnum
      {
         get;
      }
      #endregion

      public TileUndefinedException(ushort tileType)
         : base($"tile of type {tileType} is not defined")
      {
         TileType = tileType;
      }
      
      public TileUndefinedException(ushort tileType, Type tileTypeEnum)
         : base($"tile of type {tileType} is not defined by enumeration {tileTypeEnum.Name}")
      {
         TileType     = tileType;
         TileTypeEnum = tileTypeEnum;
      }
   }
   
   /// <summary>
   /// Structure that defines tile by it's type and additional state object. 
   /// </summary>
   public readonly struct Tile
   {
      #region Constant fields
      /// <summary>
      /// Constant tile type that represents an empty tile.
      /// </summary>
      public const ushort EmptyType = 0;
      
      /// <summary>
      /// Constant tile category that represents default
      /// non-existing category for the tile.
      /// </summary>
      public const ushort DefaultCategory = 0;
      #endregion
      
      #region Properties
      /// <summary>
      /// Gets the type of the tile.
      /// </summary>
      public ushort Type
      {
         get;
      }

      /// <summary>
      /// Gets optional state context of the tile. Only
      /// works with reference types as structs get copied
      /// when dereferenced.
      /// </summary>
      public object State
      {
         get;
      }
      
      /// <summary>
      /// Gets optional category flags of the tile.
      /// </summary>
      public ushort Category
      {
         get;
      }
      #endregion

      public Tile(ushort type, ushort category = DefaultCategory, object state = null)
      {
         Type     = type;
         Category = category;
         State    = state;
      }
      
      public bool IsEmpty()
         => Type == EmptyType;
      
      public bool HasCategory()
         => Category != DefaultCategory;
   }
   
   public delegate Tile TileFactoryDelegate(IGameEngine engine);

   public static class TileFactory 
   {
      #region Static fields
      private static readonly Dictionary<ushort, TileFactoryDelegate> Lookup =
         new Dictionary<ushort, TileFactoryDelegate>();
      #endregion
      
      public static void RegisterActivator(ushort type, TileFactoryDelegate factory)
      {
         if (Lookup.ContainsKey(type))
            throw new InvalidOperationException($"activator {type} already exists");
         
         Lookup.Add(type, factory);
      }
      
      public static Tile Create(IGameEngine engine, ushort type)
         => Lookup[type](engine);

      public static void RegisterDefaultActivators(IEnumerable<ushort> types)
      {
         foreach (var type in types)
            RegisterActivator(type, (engine) => new Tile(type));
      }
   }
}