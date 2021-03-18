using Fracture.Common.Events;
using Fracture.Engine.Core;

namespace Fracture.Engine.Tiles
{
   public delegate void TileMapPhysicsEventHandler(int entityId, int tileId);
   
   /// <summary>
   /// Interface for implementing tile map physics systems. Tile map physics systems
   /// handle tile based collisions and physics for entities in the tile map using
   /// the tile map system and physics systems.
   /// </summary>
   public interface ITileMapPhysicsSystem : IGameEngineSystem
   {
      #region Properties
      IEvent<int, TileMapPhysicsEventHandler> SolidCollision
      {
         get;
      }
      IEvent<int, TileMapPhysicsEventHandler> LiquidCollision
      {
         get;
      }
      #endregion
      
      /// <summary>
      /// Enables tile map based physics for given entity.
      /// </summary>
      void Enable(int entityId);
      
      /// <summary>
      /// Disables tile map based physics for given entity.
      /// </summary>
      void Disable(int entityId);
      
      void SetLiquid(int tileTypeId);
      void SetSolid(int tileTypeId);
   }
   
   public sealed class TileMapPhysicsSystem : ActiveGameEngineSystem, ITileMapPhysicsSystem
   {
      #region Fields
      
      #endregion

      #region Properties
      public IEvent<int, TileMapPhysicsEventHandler> SolidCollision
      {
         get;
      }

      public IEvent<int, TileMapPhysicsEventHandler> LiquidCollision
      {
         get;
      }
      #endregion

      public TileMapPhysicsSystem(int priority) : base(priority)
      {
      }

      public override void Update(IGameEngineTime time)
      {
         throw new System.NotImplementedException();
      }

      public void Enable(int entityId)
      {
         throw new System.NotImplementedException();
      }

      public void Disable(int entityId)
      {
         throw new System.NotImplementedException();
      }

      public void SetLiquid(int tileTypeId)
      {
         throw new System.NotImplementedException();
      }

      public void SetSolid(int tileTypeId)
      {
         throw new System.NotImplementedException();
      }
   }
}