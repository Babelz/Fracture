using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Tiles
{
   public interface ITileAttributes
   {
      
      bool IsSolid(in Tile tile);
      bool IsLiquid(in Tile tile);
      
      int LiquidSinkFactor(in Tile tile);
      
      bool DoesDamageOnHit(in Tile tile);
      int DamageOnHit(in Tile tile);
      
      bool DoesDropItem(in Tile tile);
      int DropItem(in Tile tile);
      
      bool IsFertile(in Tile tile);
      bool CanBeFertile(in Tile tile);
      bool StaysFertile(in Tile tile, TileMap map);
      
      void EntityEnter(in Tile tile, in Vector2 velocity);
      void EntityLeave(in Tile tile, in Vector2 velocity);
      
      TimeSpan TimeToDestroy(in Tile tile);
   }

   public delegate ITileAttributes TileAttributesFactoryDelegate(IGameEngine engine);

   public static class TileAttributesFactory 
   {
      #region Static fields
      private static readonly Dictionary<ushort, TileAttributesFactoryDelegate> Lookup =
         new Dictionary<ushort, TileAttributesFactoryDelegate>();
      #endregion
      
      public static void RegisterActivator(ushort type, TileAttributesFactoryDelegate factory)
      {
         if (Lookup.ContainsKey(type))
            throw new InvalidOperationException($"activator {type} already exists");
         
         Lookup.Add(type, factory);
      }
      
      public static ITileAttributes Create(IGameEngine engine, ushort type)
         => Lookup[type](engine);
   }
   
   /// <summary>
   /// Abstract base class for implementing tile attributes. Defaults all functions to default
   /// functionality.
   /// </summary>
   public abstract class TileAttributes : ITileAttributes
   {
      public TileAttributes()
      {
      }
      
      public virtual bool IsSolid(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool IsLiquid(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual int LiquidSinkFactor(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool DoesDamageOnHit(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual int DamageOnHit(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool DoesDropItem(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual int DropItem(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool IsFertile(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool CanBeFertile(in Tile tile)
      {
         throw new NotImplementedException();
      }

      public virtual bool StaysFertile(in Tile tile, TileMap map)
      {
         throw new NotImplementedException();
      }

      public virtual void EntityEnter(in Tile tile, in Vector2 velocity)
      {
         throw new NotImplementedException();
      }

      public virtual void EntityLeave(in Tile tile, in Vector2 velocity)
      {
         throw new NotImplementedException();
      }

      public virtual TimeSpan TimeToDestroy(in Tile tile)
      {
         throw new NotImplementedException();
      }
   }
}