using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class BindingValueCtorTestClass
    {
        #region Properties
        public Dep0 A
        {
            get;
        }

        public Dep1 B
        {
            get;
        }
        #endregion

        [BindingConstructor]
        public BindingValueCtorTestClass(Dep0 a, Dep1 b)
        {
            A = a;
            B = b;
        }
    }

    public sealed class BindingValuePropertyTestClass
    {
        #region Properties
        [BindingProperty]
        public Dep0 A
        {
            get;
            private set;
        }

        [BindingProperty]
        public Dep1 B
        {
            get;
            private set;
        }
        #endregion

        public BindingValuePropertyTestClass()
        {
        }
    }

    public sealed class BindingValueMethodTestClass
    {
        #region Properties
        public Dep0 A
        {
            get;
            private set;
        }

        public Dep1 B
        {
            get;
            private set;
        }
        #endregion

        public BindingValueMethodTestClass()
        {
        }

        [BindingMethod]
        private void SetA(Dep0 a)
            => A = a;

        [BindingMethod]
        private void SetB(Dep1 b)
            => B = b;
    }
}