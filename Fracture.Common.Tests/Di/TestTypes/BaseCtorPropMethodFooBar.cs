using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public abstract class BaseCtorPropMethodFooBar : FooBar
    {
        #region Properties
        [BindingProperty]
        public virtual Dep0 Dep0
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        [BindingProperty]
        public virtual Dep1 Dep1
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        [BindingProperty]
        public virtual Dep2 Dep2
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        #endregion
        
        [BindingMethod]
        public abstract void Deps0(Dep0 dep);

        [BindingMethod]
        public abstract void Deps1(Dep1 dep);

        [BindingMethod]
        public abstract void Deps2(Dep2 dep);
    }
}