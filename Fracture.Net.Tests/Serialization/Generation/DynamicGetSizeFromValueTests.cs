using System.Linq;
using Fracture.Net.Serialization.Generation;
using Xunit;

#pragma warning disable 8632

// Fields and properties are read and written dynamically, warning disabled for testing.
#pragma warning disable 649

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicGetSizeFromValueTests
    {
        #region Test types
        private sealed class NullablePropertyTestClass
        {
            #region Properties
            public int X
            {
                get;
                set;
            }

            public int? Y
            {
                get;
                set;
            }

            public int? Z
            {
                get;
                set;
            }
            #endregion
        }

        private sealed class NullableFieldTestClass
        {
            #region Fields
            public int  X;
            public int? Y;
            public int? Z;
            #endregion
        }

        private sealed class ValueTypeTestClass
        {
            #region Fields
            public int X;
            public int Y;
            public int Z;
            #endregion
        }

        private sealed class ValueTypeParametrizedActivationTestClass
        {
            #region Fields
            public int Z;

            // ReSharper disable once MemberCanBePrivate.Local
            public readonly int X;

            // ReSharper disable once MemberCanBePrivate.Local
            public readonly int Y;
            #endregion

            public ValueTypeParametrizedActivationTestClass(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private sealed class NonValueTypeTestClass
        {
            #region Fields
            public string FS;

            public int FX;
            #endregion

            #region Properties
            public string PS
            {
                get;
                set;
            }

            public int PX;
            #endregion
        }

        private sealed class NullableNonValueTypeTestClass
        {
            #region Fields
            public string? FS;

            public int? FX;
            #endregion

            #region Properties
            public string? PS
            {
                get;
                set;
            }

            public int? PX;
            #endregion
        }
        #endregion

        static DynamicGetSizeFromValueTests()
        {
            ObjectSerializerAnalyzer.Analyze(new [] { typeof(int?) });
        }

        public DynamicGetSizeFromValueTests()
        {
        }

        [Fact]
        public void Should_Compute_Size_Of_Non_Value_Type_Values_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<NonValueTypeTestClass>()
                                                   .PublicFields()
                                                   .PublicProperties()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NonValueTypeTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(NonValueTypeTestClass),
                serializationOps
            );

            Assert.Equal(11, getSizeFromValueDelegate(new NonValueTypeTestClass()));
            Assert.Equal(39, getSizeFromValueDelegate(new NonValueTypeTestClass { PS = "hello, world!" }));
            Assert.Equal(51, getSizeFromValueDelegate(new NonValueTypeTestClass { PS = "hello, world!", FS = "hello" }));
        }

        [Fact]
        public void Should_Compute_Size_Of_Non_Value_Type_Nullable_Values_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<NullableNonValueTypeTestClass>()
                                                   .PublicFields()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NullableNonValueTypeTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(NullableNonValueTypeTestClass),
                serializationOps
            );

            Assert.Equal(3, getSizeFromValueDelegate(new NullableNonValueTypeTestClass()));
            Assert.Equal(15, getSizeFromValueDelegate(new NullableNonValueTypeTestClass { FS = "hello" }));
            Assert.Equal(7, getSizeFromValueDelegate(new NullableNonValueTypeTestClass { PX  = 200 }));
            Assert.Equal(17, getSizeFromValueDelegate(new NullableNonValueTypeTestClass { FS = "s1", FX = 100, PS = "s2", PX = 200 }));
        }

        [Fact]
        public void Should_Compute_Size_Of_Nullable_Fields_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<NullableFieldTestClass>()
                                                   .PublicFields()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NullableFieldTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(NullableFieldTestClass),
                serializationOps
            );

            Assert.Equal(7, getSizeFromValueDelegate(new NullableFieldTestClass { X  = 0 }));
            Assert.Equal(11, getSizeFromValueDelegate(new NullableFieldTestClass { X = 0, Y = 200 }));
            Assert.Equal(15, getSizeFromValueDelegate(new NullableFieldTestClass { X = 0, Y = 200, Z = 400 }));
        }

        [Fact]
        public void Should_Compute_Size_Of_Nullable_Properties_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<NullablePropertyTestClass>()
                                                   .PublicProperties()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(NullablePropertyTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(NullablePropertyTestClass),
                serializationOps
            );

            Assert.Equal(7, getSizeFromValueDelegate(new NullablePropertyTestClass { X  = 0 }));
            Assert.Equal(11, getSizeFromValueDelegate(new NullablePropertyTestClass { X = 0, Y = 200 }));
            Assert.Equal(15, getSizeFromValueDelegate(new NullablePropertyTestClass { X = 0, Y = 200, Z = 400 }));
        }

        [Fact]
        public void Should_Compute_Size_Of_Value_Type_Structure_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<ValueTypeTestClass>()
                                                   .PublicFields()
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(ValueTypeTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(ValueTypeTestClass),
                serializationOps
            );

            var testObject = new ValueTypeTestClass();

            Assert.Equal(12, getSizeFromValueDelegate(testObject));
        }

        [Fact]
        public void Should_Compute_Size_Of_Value_Type_Structure_With_Parametrized_Activator_Correctly()
        {
            var mapping = ObjectSerializationMapper.ForType<ValueTypeParametrizedActivationTestClass>()
                                                   .PublicFields()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "X"),
                                                                           ObjectActivationHint.Field("y", "Y"))
                                                   .Map();

            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var valueRanges =
                ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(ValueTypeParametrizedActivationTestClass), serializationOps);

            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                valueRanges,
                typeof(ValueTypeParametrizedActivationTestClass),
                serializationOps
            );

            var testObject = new ValueTypeParametrizedActivationTestClass(0, 0);

            Assert.Equal(12, getSizeFromValueDelegate(testObject));
        }
    }
}