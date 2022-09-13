using System;
using Fracture.Common.Di.Attributes;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class PropFooBar : FooBar
    {
        #region Properties
        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used dependency binder.
        private Dep0 Dep0
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used dependency binder.
        public Dep1 Dep01
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }

        [BindingProperty]
        // ReSharper disable once UnusedMember.Local - will be used dependency binder.
        public Dep2 Dep2
        {
            get => throw new NotImplementedException();
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
            }
        }
        #endregion

        public PropFooBar()
        {
        }
    }
}