using BenchmarkDotNet.Attributes;
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
        }
        
        private sealed class NonValueTypeTestClass
        {
        }
        #endregion
    }
}