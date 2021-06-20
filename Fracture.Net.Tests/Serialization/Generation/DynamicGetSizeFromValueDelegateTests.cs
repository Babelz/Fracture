using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;
using Moq;
using Xunit;

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicGetSizeFromValueDelegateTests
    {
        #region Test types
        private sealed class ConstSizeTestClass
        {
        }
        
        private sealed class NullablePropertyTestClass
        {
        }
        
        private sealed class NullableFieldTestClass
        {
        }
        
        private sealed class MixedFieldAndPropertyTypeTestClass
        {
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
            
            public readonly int X;
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
        }
        #endregion
        
        [Fact]
        public void Should_Compute_Size_Of_Value_Type_Structure_Correctly()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ValueTypeTestClass>()
                                                   .PublicFields()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(ValueTypeTestClass),
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                context,
                typeof(ValueTypeTestClass), 
                serializationOps
            );
            
            var testObject = new ValueTypeTestClass();
            
            Assert.Equal(12, getSizeFromValueDelegate(context, testObject));
        }
        
        [Fact]
        public void Should_Compute_Size_Of_Value_Type_Structure_With_Parametrized_Activator_Correctly()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<ValueTypeParametrizedActivationTestClass>()
                                                   .PublicFields()
                                                   .ParametrizedActivation(ObjectActivationHint.Field("x", "X"),
                                                                           ObjectActivationHint.Field("y", "Y"))
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            var context = ObjectSerializerInterpreter.InterpretObjectSerializationContext(
                typeof(ValueTypeParametrizedActivationTestClass),
                serializationOps, 
                ObjectSerializerProgram.GetOpSerializers(serializationOps).ToList().AsReadOnly()
            );
            
            var getSizeFromValueDelegate = ObjectSerializerInterpreter.InterpretDynamicGetSizeFromValueDelegate(
                context,
                typeof(ValueTypeParametrizedActivationTestClass), 
                serializationOps
            );
            
            var testObject = new ValueTypeParametrizedActivationTestClass(0, 0);
            
            Assert.Equal(12, getSizeFromValueDelegate(context, testObject));
        }
    }
}