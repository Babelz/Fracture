using System;

namespace Fracture.Common.Reflection
{
    /// <summary>
    /// Class that wraps dynamic property get and set methods.
    /// </summary>
    public sealed class DynamicPropertyWrapper
    {
        #region Fields
        private readonly Action<object, object> set;
        private readonly Func<object, object> get;
        #endregion

        #region Properties
        public string Name
        {
            get;
        }
        #endregion

        public DynamicPropertyWrapper(Type type, string name)
        {
            Name = name;

            set = DynamicPropertyBinder.BindSet(type, name);
            get = DynamicPropertyBinder.BindGet(type, name);
        }

        public object Get(object target) => get?.Invoke(target);

        public void Set(object target, object value) => set?.Invoke(target, value);
    }
}