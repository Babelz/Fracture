using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class CtroFooBar : FooBar
    {
        [BindingConstructor]
        public CtroFooBar(Dep0 dep0, Dep1 dep1, Dep2 dep2)
        {
            if (dep0 == null) throw new ArgumentNullException(nameof(dep0));
            if (dep1 == null) throw new ArgumentNullException(nameof(dep1));
            if (dep2 == null) throw new ArgumentNullException(nameof(dep2));
        }
    }
    
    public sealed class OptionalCtroFooBar : FooBar
    {
        #region Properties
        public Dep0 Dep0
        {
            get;
        }

        public Dep1 Dep1
        {
            get;
        }

        public Dep2 Dep2
        {
            get;
        }
        #endregion

        [BindingConstructor]
        public OptionalCtroFooBar(Dep0 dep0 = null, Dep1 dep1 = null, Dep2 dep2 = null)
        {
            Dep0 = dep0;
            Dep1 = dep1;
            Dep2 = dep2;
        }
    }
}
