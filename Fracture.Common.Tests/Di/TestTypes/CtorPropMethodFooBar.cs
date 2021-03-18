using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class CtorPropMethodFooBar : FooBar
    {
        #region Properties
        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used by DI.
        private Dep0 Dep0
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used by DI.
        public Dep1 Dep01
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used by DI.
        public Dep2 Dep2
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }
        #endregion

        [BindingConstructor]
        public CtorPropMethodFooBar(Dep0 dep0, Dep1 dep1, Dep2 dep2)
        {
            if (dep0 == null) throw new ArgumentNullException(nameof(dep0));
            if (dep1 == null) throw new ArgumentNullException(nameof(dep1));
            if (dep2 == null) throw new ArgumentNullException(nameof(dep2));
        }

        [BindingMethod]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        private void Deps0(Dep0 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        public void Deps1(Dep1 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }

        [BindingMethod]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - tests that DI passes a value.
        // ReSharper disable once UnusedMember.Local - invoked by DI.
        public void Deps2(Dep2 dep)
        {
            if (dep == null) throw new ArgumentNullException(nameof(dep));
        }
    }
}
