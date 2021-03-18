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
}
