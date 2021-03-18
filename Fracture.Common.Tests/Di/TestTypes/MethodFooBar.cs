using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class MethodFooBar : FooBar
    {
        public MethodFooBar()
        {
        }

        [BindingMethod]
        private void Deps0(Dep0 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        public void Deps1(Dep1 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        public void Deps2(Dep2 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }
    }
}
