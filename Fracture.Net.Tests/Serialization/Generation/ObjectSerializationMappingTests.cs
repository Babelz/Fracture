#pragma warning disable 649 - used for testing and the value is dynamically discovered
#pragma warning disable 169 - used for testing and the value is dynamically discovered

using Fracture.Net.Serialization.Generation;
using Xunit;

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public sealed class ObjectSerializationMappingTests
    {
        #region Test types
        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private class NoDefaultConstructorTestClass
        {
            #region Properties
            public int Foo
            {
                get;
                set;
            }
            #endregion

            public NoDefaultConstructorTestClass(int foo)
                => Foo = foo;
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private class DefaultConstructorTestClass
        {
            #region Fields
            public int Foo;
            #endregion

            public DefaultConstructorTestClass()
            {
            }
        }
        
        private abstract class AbstractTestClass
        {
        }
        
        private interface ITestInterface
        {
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class ReadOnlyPropertyTestClass
        {
            #region Properties
            public int Foo
            {
                get;
            } = 0;
            #endregion
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class WriteOnlyPropertyTestClass
        {
            #region Fields
            private int foo = 0;
            #endregion

            #region Properties
            public int Foo
            {
                set => foo = value;
            }
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class EmptyTestClass
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class StaticFieldTestClass
        {
            #region Fields
            public static int j;
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class StaticPropertyTestClass
        {
            #region Properties
            public static int Foo
            {
                get;
                set;
            }
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class ReadOnlyFieldTestClass
        {
            #region Fields
            private readonly int x;
            #endregion

            public ReadOnlyFieldTestClass()
            {
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class ParametrizedReadOnlyFieldTestClass
        {
            #region Fields
            public readonly int x;
            #endregion

            public ParametrizedReadOnlyFieldTestClass(int x)
                => this.x = x;
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class ParametrizedReadOnlyPropertyTestClass
        {
            #region Properties
            // ReSharper disable once MemberCanBePrivate.Local
            public int X
            {
                get;
            }
            #endregion

            public ParametrizedReadOnlyPropertyTestClass(int x)
                => X = x;
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class PublicFieldTestClass
        {
            #region Fields
            private int x1;

            public int x2;
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class PublicPropertyTestClass
        {
            #region Properties
            private int Foo1
            {
                get;
                set;
            }
            
            public int Foo2
            {
                get;
                set;
            }
            #endregion
        }
        
        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class NullableTestClass
        {
            #region Fields
            public int? MaybeNumber;
            #endregion
            
            #region Properties
            public byte? MaybeByte
            {
                get;
                set;
            }
            #endregion
        }
        #endregion
        
        [Fact]
        public void Should_Throw_If_No_Default_Constructor_Exists()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<NoDefaultConstructorTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("no parameterless constructor", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Constructor_Does_Not_Exist()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<NoDefaultConstructorTestClass>()
                                                                            .PublicProperties()
                                                                            .ParametrizedActivation(new []
                                                                             {
                                                                                 ObjectActivationHint.Property("bar", "Bar")
                                                                             })
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("that accepts 1 arguments", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Left_Null()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't map null type", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Abstract()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<AbstractTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't map abstract type", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Interface()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<ITestInterface>()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't map interface type", exception.Message);
        }
        
        [Fact]
        public void Should_Map_Correctly_To_Default_Constructor()
        {            
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<DefaultConstructorTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            Assert.True(mapping.Activator.IsDefaultConstructor);
        }
        
        [Fact]
        public void Should_Map_Correctly_To_Parametrized_Constructor()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NoDefaultConstructorTestClass>()
                                                   .ParametrizedActivation(new []
                                                    {
                                                        ObjectActivationHint.Property("foo", "Foo")
                                                    })
                                                   .PublicProperties()
                                                   .Map();
            
            Assert.False(mapping.Activator.IsDefaultConstructor);
            Assert.Contains(mapping.Activator.Values, f => f.IsProperty && f.Name == "Foo");
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Readonly()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<ReadOnlyPropertyTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't be used for writing", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Write_Only()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<WriteOnlyPropertyTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't be used for reading", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Field_Is_Readonly()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<ReadOnlyFieldTestClass>()
                                                                            .Values(new []
                                                                             {
                                                                                 SerializationValueHint.Field("x")
                                                                             })
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("can't serialize readonly field", exception.Message);
        }
        
        [Fact]
        public void Should_Allow_Readonly_Fields_When_They_Are_Used_With_Object_Activation()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ParametrizedReadOnlyFieldTestClass>()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "x"))
                                                   .Map();
            
            Assert.DoesNotContain(mapping.Values, f => f.Name == "x");
            Assert.Contains(mapping.Activator.Values, f => f.Name == "x");
        }
        
        [Fact]
        public void Should_Allow_Readonly_Properties_When_They_Are_Used_With_Object_Activation()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ParametrizedReadOnlyPropertyTestClass>()
                                                   .ParametrizedActivation(ObjectActivationHint.Property("x", "X"))
                                                   .Map();
            
            Assert.DoesNotContain(mapping.Values, f => f.Name == "X");
            Assert.Contains(mapping.Activator.Values, f => f.Name == "X");
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Field_Is_Static()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<StaticFieldTestClass>()
                                                                            .Values(SerializationValueHint.Field("x"))
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("no field matches serialization field hint", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Static()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<StaticPropertyTestClass>()
                                                                            .Values(SerializationValueHint.Property("Foo"))
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("no property matches serialization field hint", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Field_Does_Not_Exist()
        {            
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<EmptyTestClass>()
                                                                            .Values(SerializationValueHint.Field("x"))
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("no field matches serialization field hint", exception.Message);
        }
        
        [Fact]
        public void Should_Throw_If_Serialization_Property_Does_Not_Exist()
        {            
            var exception = Record.Exception(() => ObjectSerializationMapper.Create()
                                                                            .FromType<EmptyTestClass>()
                                                                            .Values(SerializationValueHint.Property("Foo"))
                                                                            .Map());
            
            Assert.NotNull(exception);
            Assert.Contains("no property matches serialization field hint", exception.Message);
        }
        
        [Fact]
        public void Should_Map_All_Public_Fields()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<PublicFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            Assert.Contains(mapping.Values, f => f.Name == "x2");
            Assert.DoesNotContain(mapping.Values, f => f.Name == "x1");
            
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "x1");
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "x2");
        }
        
        [Fact]
        public void Should_Map_All_Public_Properties()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<PublicPropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();
            
            Assert.DoesNotContain(mapping.Values, f => f.Name == "Foo1");
            Assert.Contains(mapping.Values, f => f.Name == "Foo2");
            
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "Foo1");
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "Foo2");
        }
        
        [Fact]
        public void Should_Nullable_Types()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<NullableTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();
            
            Assert.Contains(mapping.Values, f => f.Name == "MaybeNumber");
            Assert.Contains(mapping.Values, f => f.Name == "MaybeByte");
        }
    }
}