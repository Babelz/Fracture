using System;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public class FooBar : IFoo, IBar
    {
        public FooBar()
        {
        }

        public void Bar()
            => throw new NotImplementedException();

        public void Foo()
            => throw new NotImplementedException();
    }
}