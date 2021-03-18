using System;

namespace Fracture.Common.Collections
{
   /// <summary>
   /// Container that works as unique, read/write storage that
   /// allows writing once.
   /// </summary>
   public sealed class LinearRegistry<T>
   {
      #region Constant fields
      private const int InitialCapacity = 8;
      #endregion

      #region Fields
      private T[] items;
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

         if (items[location]?.Equals(default(T)) ?? false)
            throw new InvalidOperationException($"index {location} already reserved");
         
         items[location] = item;
      }

      public bool IsReserved(int index)
         => index >= 0 && index < items.Length && !items[index].Equals(default(T)); 
      
      public ref T AtLocation(int location) 
         => ref items[location];
   }
}