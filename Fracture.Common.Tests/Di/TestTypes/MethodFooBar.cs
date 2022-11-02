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
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        private void Deps0(Dep0 dep)
        {
            if (dep == null)
                throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        public void Deps1(Dep1 dep)
        {
            if (dep == null)
                throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        public void Deps2(Dep2 dep)
        {
            if (dep == null)
                throw new ArgumentNullException(nameof(dep));
        }
    }
}