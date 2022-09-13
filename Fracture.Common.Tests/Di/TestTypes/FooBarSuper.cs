using System;

namespace Fracture.Common.Tests.Di.TestTypes
{
    public sealed class FooBarSuper : FooBar
    {
        public FooBarSuper()
        {
        }

        public void SuperFooBar()
        {
            throw new NotImplementedException();
        }
    }
}