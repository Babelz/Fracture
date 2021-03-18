using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class CtorPropMethodFooBar : FooBar
    {
        #region Properties
        [BindingProperty]
        private Dep0 Dep0
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
        public Dep1 Dep01
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
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
