using System.Collections.Generic;
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
                // ReSharper disable once ObjectCreationAsStatement - disabled for testing constructor.
                new Kernel();
            }
            catch
            {
                Assert.True(false, "constructor should not throw");
            }
        }
        
        [Fact]
        public void Bind_Instance_With_Binding_Values_Using_Binding_Ctor_Test()
        {
            var kernel = new Kernel();
            
            kernel.Bind<Dep1>();
            
            var dep0 = new Dep0();
            
            kernel.Bind<BindingValueCtorTestClass>(BindingValue.Const("a", dep0));
            
            var result = kernel.First<BindingValueCtorTestClass>();
            
            Assert.Same(dep0, result.A);
            Assert.NotNull(result.B);
        }

        [Fact]
        public void Bind_Instance_With_Binding_Values_Using_Binding_Property_Test()
        {
            var kernel = new Kernel();
            
            kernel.Bind<Dep1>();
            
            var dep0 = new Dep0();
            
            kernel.Bind<BindingValuePropertyTestClass>(BindingValue.Const("A", dep0));
            
            var result = kernel.First<BindingValuePropertyTestClass>();
            
            Assert.Same(dep0, result.A);
            Assert.NotNull(result.B);
        }

        [Fact]
        public void Bind_Instance_With_Binding_Values_Using_Binding_Method_Test()
        {
            var kernel = new Kernel();
            
            kernel.Bind<Dep1>();
            
            var dep0 = new Dep0();
            
            kernel.Bind<BindingValueMethodTestClass>(BindingValue.Const("a", dep0));
            
            var result = kernel.First<BindingValueMethodTestClass>();
            
            Assert.Same(dep0, result.A);
            Assert.NotNull(result.B);
        }

        [Fact]
        public void Bind_Instance_Test()
        {
            var kernel = new Kernel();

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Type_Test()
        {
            var kernel = new Kernel();

            kernel.Bind(typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_Instance_Non_Strict_test()
        {
            var kernel = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Interfaces);

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_Instance_With_Options_And_Strict()
        {
            var kernel = new Kernel(DependencyBindingOptions.Class | DependencyBindingOptions.Strict);

            kernel.Bind(new FooBar());

            Assert.True(kernel.Exists<FooBar>());
            Assert.False(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Type_And_Options_Test()
        {
            var kernel = new Kernel(DependencyBindingOptions.Interfaces);

            kernel.Bind(typeof(FooBar));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }

        [Fact]
        public void Bind_With_Instance_Proxy_Test()
        {
            var kernel = new Kernel();

            kernel.Proxy(new FooBar(), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Type_Proxy_Test()
        {
            var kernel = new Kernel();

            kernel.Proxy(typeof(FooBar), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Instance_Proxy_Using_Options_Test()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Interfaces);

            kernel.Proxy(new FooBar(), typeof(IFoo));

            Assert.False(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Type_Proxy_Using_Options_And_Non_Strict_Test()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Class);

            kernel.Proxy(typeof(FooBarSuper), typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.True(kernel.Exists<IFoo>());
            Assert.True(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Bind_With_Type_Proxy_Using_Options_And_Strict_Test()
        {
            var kernel = new Kernel(proxyOptions: DependencyBindingOptions.Class | DependencyBindingOptions.Strict);

            kernel.Proxy(typeof(FooBarSuper), typeof(FooBar));

            Assert.True(kernel.Exists<FooBar>());
            Assert.False(kernel.Exists<IFoo>());
            Assert.False(kernel.Exists<IBar>());
        }
        
        [Fact]
        public void Dependencies_Are_Activated_With_Available_Binding_Constructor()
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
        public void Dependency_Binding_Properties_Are_Filled_After_Activation()
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
        public void Dependency_Binding_Methods_Are_Invoked_After_Activation()
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
        public void Binding_Methods_And_Properties_Are_Processed_After_Activation()
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
        public void Virtual_Methods_And_Properties_Are_Called_Correctly()
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
        public void Binding_Directly_To_Interface_Throws()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Bind<IFoo>());
        }
        
        [Fact]
        public void Binding_Directly_To_Abstract_Classes_Throws()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Bind<BaseCtorPropMethodFooBar>());
        }
        
        [Fact]
        public void Activate_Throws_If_Dependencies_Are_Not_Present()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Activate<PropFooBar>());
        }
        
        [Fact]
        public void Activate_Throws_If_Dependencies_Are_Not_Activated()
        {
            var kernel = new Kernel();

            Assert.Throws<DependencyBinderException>(() => kernel.Activate<CtroFooBar>());
        }
        
        [Fact]
        public void Binding_Works_With_Default_Parameters()
        {
            var dep2   = new Dep2();
            var kernel = new Kernel();
            
            kernel.Bind(dep2);
            kernel.Bind<OptionalCtroFooBar>();
            
            var value = kernel.First<OptionalCtroFooBar>();
            
            Assert.Null(value.Dep0);
            Assert.Null(value.Dep1);
            Assert.Equal(dep2, value.Dep2);
        }
        
        [Fact]
        public void Activate_Works_With_Default_Parameters()
        {
            var dep1   = new Dep1();
            var kernel = new Kernel();
            var value  = kernel.Activate<OptionalCtroFooBar>(BindingValue.Const("dep1", dep1));
            
            Assert.Null(value.Dep0);
            Assert.Equal(dep1, value.Dep1);
            Assert.Null(value.Dep2);
        }
    }
}
