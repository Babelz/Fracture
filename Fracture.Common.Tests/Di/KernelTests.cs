using Fracture.Common.Di;
using Fracture.Common.Di.Binding;
using Fracture.Common.Tests.Di.TestTypes;
using Xunit;

namespace Fracture.Common.Tests.Di
{
    [Trait("Category", "Dependency injection")]
    public class KernelTests
    {
        [Fact]
        public void Kernel_Ctor_Test()
        {
            try
            {
                new Kernel();
            }
            catch
            {
                Assert.True(false, "constructor should not throw");
            }
        }
        
        [Fact]
        public void BindWithInstanceTest()
        {
            var kernel = new Kernel();

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeTest()
        {
            var kernel = new Kernel();

            kernel.Bind(typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithInstanceAndOptionsNonStrictTest()
        {
            var kernel = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Interfaces);

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithInstanceAndOptionsStrictTest()
        {
            var kernel = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Strict);

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.False(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeAndOptionsTest()
        {
            var kernel = new Kernel(DependencyBindingOptions.Interfaces);

            kernel.Bind(typeof(FooBar));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeGenericTest()
        {
            var kernel = new Kernel();

            kernel.Bind<FooBar>();

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeGenericAndOptionsTest()
        {
            var kernel = new Kernel(DependencyBindingOptions.Interfaces);

            kernel.Bind<FooBar>();

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithInstanceProxyTests()
        {
            var kernel = new Kernel();

            kernel.Proxy(new FooBar(), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeProxyTests()
        {
            var kernel = new Kernel();

            kernel.Proxy(typeof(FooBar), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithInstanceProxyAndOptionsTest()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Interfaces);

            kernel.Proxy(new FooBar(), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeProxyAndOptionsNonStrictTests()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Class);

            kernel.Proxy(typeof(FooBarSuper), typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindWithTypeProxyAndOptionsStrictTests()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Class | DependencyBindingOptions.Strict);

            kernel.Proxy(typeof(FooBarSuper), typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.False(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindingConstructorTest()
        {
            var kernel = new Kernel();

            kernel.Bind<CtroFooBar>();

            Assert.False(kernel.Exists<CtroFooBar>());

            kernel.Bind(new Dep0());
            Assert.False(kernel.Exists<CtroFooBar>());

            kernel.Bind(new Dep1());
            Assert.False(kernel.Exists<CtroFooBar>());

            kernel.Bind(new Dep2());

            Assert.True(kernel.Exists<CtroFooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindingPropertyTest()
        {
            var kernel = new Kernel();

            kernel.Bind<PropFooBar>();

            Assert.False(kernel.Exists<PropFooBar>());

            kernel.Bind(new Dep0());
            Assert.False(kernel.Exists<PropFooBar>());

            kernel.Bind(new Dep1());
            Assert.False(kernel.Exists<PropFooBar>());

            kernel.Bind(new Dep2());

            Assert.True(kernel.Exists<PropFooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindingMethodTest()
        {
            var kernel = new Kernel();

            kernel.Bind<MethodFooBar>();

            Assert.False(kernel.Exists<MethodFooBar>());

            kernel.Bind(new Dep0());
            Assert.False(kernel.Exists<MethodFooBar>());

            kernel.Bind(new Dep1());
            Assert.False(kernel.Exists<MethodFooBar>());

            kernel.Bind(new Dep2());

            Assert.True(kernel.Exists<MethodFooBar>());
            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindingConstructorPropertyAndMethodTest()
        {
            var kernel = new Kernel();

            kernel.Bind<CtorPropMethodFooBar>();

            Assert.False(kernel.Exists<CtorPropMethodFooBar>());

            kernel.Bind(new Dep0());
            Assert.False(kernel.Exists<CtorPropMethodFooBar>());

            kernel.Bind(new Dep1());
            Assert.False(kernel.Exists<CtorPropMethodFooBar>());

            kernel.Bind(new Dep2());

            Assert.True(kernel.Exists<CtorPropMethodFooBar>());
            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void BindingConstructorVirtualPropertyAndMethodTest()
        {
            var kernel = new Kernel();

            kernel.Bind<BaseCtorPropMethodFooBarImpl>();

            Assert.False(kernel.Exists<BaseCtorPropMethodFooBarImpl>());

            kernel.Bind(new Dep0());
            Assert.False(kernel.Exists<BaseCtorPropMethodFooBarImpl>());

            kernel.Bind(new Dep1());
            Assert.False(kernel.Exists<BaseCtorPropMethodFooBarImpl>());

            kernel.Bind(new Dep2());

            Assert.True(kernel.Exists<BaseCtorPropMethodFooBarImpl>());
            Assert.True(kernel.Exists<BaseCtorPropMethodFooBar>());
            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void CantBindToInterfaceTest()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Bind<IFoo>());
        }
        
        [Fact]
        public void CantBindToAbstractClassTest()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Bind<BaseCtorPropMethodFooBar>());
        }
    }
}
