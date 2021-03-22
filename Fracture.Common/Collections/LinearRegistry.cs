using System;
using System.Collections.Generic;
using System.Linq;

namespace Fracture.Common.Collections
{
   /// <summary>
   /// Container that works as unique read/write storage.
   ///
   /// TODO: optimize access, make complete data structure out of this.
   /// </summary>
   public sealed class LinearRegistry<T>
   {
      #region Constant fields
      private const int InitialCapacity = 8;
      #endregion

      #region Fields
      private T[] items;
      #endregion

      #region Properties
      public IEnumerable<T> Values => items.Where(i => i != null);
      #endregion

      public LinearRegistry()
         => items = new T[InitialCapacity];
      
      /// <summary>
      /// Registers item to given location. If the location
      /// is already reserved, will throw and exception.
      /// </summary>
      public void Register(int location, in T item)
      {
         if (location < 0)
            throw new ArgumentOutOfRangeException(nameof(location));
         
         // Grow in factor of two.
         while (location >= items.Length)
            Array.Resize(ref items, items.Length * 2);

         items[location] = item;
      }

      public bool IsReserved(int index)
         => index >= 0 && index < items.Length && !items[index].Equals(default(T)); 
      
      public ref T AtLocation(int location) 
         => ref items[location];
   }
}