using System.Linq;
using Fracture.Net.Serialization.Generation;
using Xunit;

// Used for testing and the value is dynamically discovered.
#pragma warning disable 649
#pragma warning disable 169

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
            public static int J;
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
            public readonly int X;
            #endregion

            public ParametrizedReadOnlyFieldTestClass(int x)
                => X = x;
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

            public int X2;
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

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class MixedNullableTestClass
        {
            #region Fields
            public int  X1;
            public int? X2;
            public int  X3;
            public int? X4;
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local - only used in testing and the type is dynamically discovered.
        private sealed class ActivationTestClass
        {
            #region Fields
            public int I;
            public int J;

            // ReSharper disable once MemberCanBePrivate.Local
            public int X;

            // ReSharper disable once MemberCanBePrivate.Local
            public int Y;

            // ReSharper disable once MemberCanBePrivate.Local
            public int? K;
            #endregion

            public ActivationTestClass(int x, int y, int? k)
            {
                X = x;
                Y = y;
                K = k;
            }
        }
        #endregion

        public ObjectSerializationMappingTests()
        {
        }

        [Fact]
        public void Should_Throw_If_No_Default_Constructor_Exists()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<NoDefaultConstructorTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("no parameterless constructor", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Constructor_Does_Not_Exist()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<NoDefaultConstructorTestClass>()
                                                                            .PublicProperties()
                                                                            .ParametrizedActivation(ObjectActivationHint.Property("bar", "Bar"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("that accepts 1 arguments", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Left_Null()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType(null)
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("Value cannot be null", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Abstract()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<AbstractTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("can't map abstract type", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Type_Is_Interface()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<ITestInterface>()
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("can't map interface type", exception.Message);
        }

        [Fact]
        public void Should_Map_Correctly_To_Default_Constructor()
        {
            var mapping = ObjectSerializationMapper.ForType<DefaultConstructorTestClass>()
                                                   .PublicFields()
                                                   .Map();

            Assert.True(mapping.Activator.IsDefaultConstructor);
        }

        [Fact]
        public void Should_Map_Correctly_To_Parametrized_Constructor()
        {
            var mapping = ObjectSerializationMapper.ForType<NoDefaultConstructorTestClass>()
                                                   .ParametrizedActivation(ObjectActivationHint.Property("foo", "Foo"))
                                                   .PublicProperties()
                                                   .Map();

            Assert.False(mapping.Activator.IsDefaultConstructor);
            Assert.Contains(mapping.Activator.Values, f => f.IsProperty && f.Name == "Foo");
        }

        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Readonly()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<ReadOnlyPropertyTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("can't be used for writing", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Write_Only()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<WriteOnlyPropertyTestClass>()
                                                                            .PublicProperties()
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("can't be used for reading", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Field_Is_Readonly()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<ReadOnlyFieldTestClass>()
                                                                            .Values(SerializationValueHint.Field("x"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("can't serialize readonly field", exception.Message);
        }

        [Fact]
        public void Should_Allow_Readonly_Fields_When_They_Are_Used_With_Object_Activation()
        {
            var mapping = ObjectSerializationMapper.ForType<ParametrizedReadOnlyFieldTestClass>()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "X"))
                                                   .Map();

            Assert.DoesNotContain(mapping.Values, f => f.Name == "X");
            Assert.Contains(mapping.Activator.Values, f => f.Name == "X");
        }

        [Fact]
        public void Should_Allow_Readonly_Properties_When_They_Are_Used_With_Object_Activation()
        {
            var mapping = ObjectSerializationMapper.ForType<ParametrizedReadOnlyPropertyTestClass>()
                                                   .ParametrizedActivation(ObjectActivationHint.Property("x", "X"))
                                                   .Map();

            Assert.DoesNotContain(mapping.Values, f => f.Name == "X");
            Assert.Contains(mapping.Activator.Values, f => f.Name == "X");
        }

        [Fact]
        public void Should_Throw_If_Serialization_Field_Is_Static()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<StaticFieldTestClass>()
                                                                            .Values(SerializationValueHint.Field("x"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("no field matches serialization field hint", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Property_Is_Static()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<StaticPropertyTestClass>()
                                                                            .Values(SerializationValueHint.Property("Foo"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("no property matches serialization field hint", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Field_Does_Not_Exist()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<EmptyTestClass>()
                                                                            .Values(SerializationValueHint.Field("x"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("no field matches serialization field hint", exception.Message);
        }

        [Fact]
        public void Should_Throw_If_Serialization_Property_Does_Not_Exist()
        {
            var exception = Record.Exception(() => ObjectSerializationMapper.ForType<EmptyTestClass>()
                                                                            .Values(SerializationValueHint.Property("Foo"))
                                                                            .Map());

            Assert.NotNull(exception);
            Assert.Contains("no property matches serialization field hint", exception.Message);
        }

        [Fact]
        public void Should_Map_All_Public_Fields()
        {
            var mapping = ObjectSerializationMapper.ForType<PublicFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();

            Assert.Contains(mapping.Values, f => f.Name == "X2");
            Assert.DoesNotContain(mapping.Values, f => f.Name == "X1");

            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "X1");
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "X2");
        }

        [Fact]
        public void Should_Map_All_Public_Properties()
        {
            var mapping = ObjectSerializationMapper.ForType<PublicPropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();

            Assert.DoesNotContain(mapping.Values, f => f.Name == "Foo1");
            Assert.Contains(mapping.Values, f => f.Name == "Foo2");

            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "Foo1");
            Assert.DoesNotContain(mapping.Activator.Values, f => f.Name == "Foo2");
        }

        [Fact]
        public void Should_Map_Nullable_Types()
        {
            var mapping = ObjectSerializationMapper.ForType<NullableTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .Map();

            Assert.Contains(mapping.Values, f => f.Name == "MaybeNumber");
            Assert.Contains(mapping.Values, f => f.Name == "MaybeByte");
        }

        [Fact]
        public void Should_Order_Activation_Values_First()
        {
            var mapping = ObjectSerializationMapper.ForType<ActivationTestClass>()
                                                   .PublicProperties()
                                                   .PublicFields()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "X"),
                                                                           ObjectActivationHint.Field("y", "Y"),
                                                                           ObjectActivationHint.Field("k", "K"))
                                                   .Map();

            Assert.Equal("X", mapping.Activator.Values.ElementAt(0).Name);
            Assert.Equal("Y", mapping.Activator.Values.ElementAt(1).Name);
            Assert.Equal("K", mapping.Activator.Values.ElementAt(2).Name);
            Assert.Equal("I", mapping.Values.ElementAt(0).Name);
            Assert.Equal("J", mapping.Values.ElementAt(1).Name);
        }
    }
}