using System;
using System.Collections.Generic;
using Fracture.Common.Collections;
using Fracture.Common.Memory;
using Fracture.Common.Memory.Pools;
using Fracture.Common.Memory.Storages;

namespace Fracture.Common.Events
{
    /// <summary>
    /// Interface for marking structure types used for struct event handlers.
    /// </summary>
    public interface IStructEventArgs
    {
        // Marker interface, nothing to implement.
    }
    
    /// <summary>
    /// Default delegate type for creating structure based event handlers.
    /// </summary>
    public delegate void StructEventHandler<T>(object sender, in T e) where T : struct, IStructEventArgs;
    
    public abstract class PooledEventArgs<T> : EventArgs, IClearable where T : EventArgs, IClearable, new() 
    {
        #region Static fields
        private static readonly Dictionary<Type, IPool<T>> Pools;
        #endregion

        static PooledEventArgs()
            => Pools = new Dictionary<Type, IPool<T>>();

        protected PooledEventArgs()
        {
        }
        
        public static T Take(PoolElementDecoratorDelegate<T> decorator = null)
        {
            lock (Pools)
            {
                if (Pools.TryGetValue(typeof(T), out var pool)) return pool.Take();
                
                pool = new CleanPool<T>(new Pool<T>(new LinearStorageObject<T>(new LinearGrowthArray<T>())));
                    
                Pools.Add(typeof(T), pool);

                var element = pool.Take();
                
                decorator?.Invoke(element);
                
                return element;
            }
        }
        
        public static void Return(T element)
        {
            lock (Pools)
                Pools[typeof(T)].Return(element);
        }
        
        public virtual void Clear()
        {
        }
    }
}