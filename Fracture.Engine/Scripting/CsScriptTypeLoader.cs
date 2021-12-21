using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;

namespace Fracture.Engine.Scripting
{
    /// <summary>
    /// Static utility class that contains CS script loading and type related methods.
    /// </summary>
    public static class CsScriptTypeLoader
    {
        #region Static fields
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion
        
        /// <summary>
        /// Attempts to get <see cref="CsScriptDefinitionAttribute"/> from given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetScriptDefinitionAttribute(Type type, out CsScriptDefinitionAttribute definition)
        {
            definition = type.GetCustomAttribute<CsScriptDefinitionAttribute>();
            
            return definition != null;
        }

        /// <summary>
        /// Loads all types that have matching <see cref="CsScriptDefinitionAttribute"/> present
        /// from all loaded assemblies.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Type> GetScriptTypes(CsScriptDefinitionAttribute definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var types = new List<Type>();

            // Load classes that have the same attribute and are not abstract.
            Parallel.ForEach(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
            {
                try
                {
                    types.AddRange(assembly.GetTypes()
                                           .Where(t => !t.IsAbstract && 
                                                       t.IsClass && 
                                                       t.GetCustomAttributes().Contains(definition) &&
                                                       t.GetCustomAttribute<CsScriptIgnoreAttribute>() == null));   
                }
                catch (ReflectionTypeLoadException e)
                {
                    Log.Warn(e, $"{nameof(ReflectionTypeLoadException)} occured while loading assemblies");
                }
            });
            
            return types;
        }

        /// <summary>
        /// Attempts to get <see cref="CsScriptAttribute"/> from given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetScriptAttribute(Type type, out CsScriptAttribute script)
        {
            script = type.GetCustomAttribute<CsScriptAttribute>();

            return script != null;
        }
    }
}
