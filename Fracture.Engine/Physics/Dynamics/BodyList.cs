using System;
using System.Collections.Generic;
using Fracture.Common.Collections;
using Microsoft.Xna.Framework;

namespace Fracture.Engine.Physics.Dynamics
{
   /// <summary>
   /// Interface that provides readable and mutation operations for body list. 
   /// </summary>
   public interface IBodyList : IReadOnlyBodyList
   {
      /// <summary>
      /// Allocates new body to the body list.
      /// </summary>
      /// <returns></returns>
      int Create(BodyType type, in Vector2 position, float rotation, in Shape shape, object userData = null);
      
      /// <summary>
      /// Deletes body with given id from the list.
      /// </summary>
      void Delete(int id);
   }
   
   /// <summary>
   /// Interface that provides read operations for body lists.
   /// </summary>
   public interface IReadOnlyBodyList
   {
      #region Properties
      int Count
      {
         get;
      }
      #endregion

      ref Body WithId(int id);
      
      ref Body AtIndex(int index);
   }
   
   /// <summary>
   /// Class that provides body management. Optimized for linear and non-linear lookups.
   /// </summary>
   public sealed class BodyList : IBodyList
   {
      #region Fields
      private readonly FreeList<int> ids;
      
      private readonly LinearGrowthArray<Body> bodies;
      
      private readonly List<int> indices;
      #endregion
      
      #region Properties
      public int Count => indices.Count;
      #endregion

      public BodyList()
      {
         var idc = 0;
         
         ids     = new FreeList<int>(() => idc++);
         bodies  = new LinearGrowthArray<Body>(256);
         indices = new List<int>();
      }
      
      public int Create(BodyType type, in Vector2 position, float rotation, in Shape shape, object userData = null)
      {
         var id = ids.Take();
         
         indices.Add(id);
         
         bodies.Insert(id, new Body(id, type, position, rotation, shape, userData));
         
         return id;
      }
      
      public void Delete(int id)
      {
         if (!indices.Remove(id)) 
            throw new InvalidOperationException($"body with id {id} does not exist");
         
         bodies.Insert(id, default);
         
         ids.Return(id);
      }
      
      public ref Body WithId(int id)
         => ref bodies.AtIndex(id);
      
      public ref Body AtIndex(int index)
         => ref bodies.AtIndex(indices[index]);
   }
}