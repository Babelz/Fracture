using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fracture.Engine.Tiles
{
   /// <summary>
   /// Attribute that marks tile map style class. Tile map themes should be loaded once. 
   /// </summary>
   [AttributeUsage(AttributeTargets.Class)]
   public sealed class TileMapThemeAttribute : Attribute
   {
      #region Properties
      public string Name
      {
         get;
      }
      #endregion
      
      public TileMapThemeAttribute(string name)
         => Name = !string.IsNullOrEmpty(name) ? name : throw new ArgumentNullException(nameof(name));
   }
   
   /// <summary>
   /// Attribute that marks the loading method or function of single tile map theme.
   /// </summary>
   [AttributeUsage(AttributeTargets.Method)]
   public sealed class TileMapThemeLoadAttribute : Attribute
   {
      public TileMapThemeLoadAttribute()
      {
      }
   }
   
   /// <summary>
   /// Static utility class for loading tile map themes.
   /// </summary>
   public static class TileMapThemeLoader
   {
      /// <summary>
      /// Load single theme.
      /// <param name="themeType">type that contains the theme</param>
      /// </summary>
      public static void Load(Type themeType)
      {
         var loadMethod = themeType.GetMethods().First(m => m.GetCustomAttribute<TileMapThemeLoadAttribute>() != null);

         loadMethod.Invoke(loadMethod.IsStatic ? null : Activator.CreateInstance(themeType), null);   
      }
      
      /// <summary>
      /// Load themes from given assemblies.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Load(IEnumerable<Assembly> assemblies)
      {
         var themeTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<TileMapThemeAttribute>() != null);
         
         foreach (var themeType in themeTypes)
            Load(themeType);
      }
      
      /// <summary>
      /// Load themes from all loaded assemblies.
      /// </summary>
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static void Load()
         => Load(AppDomain.CurrentDomain.GetAssemblies());
   }
}