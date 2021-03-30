using System;
using System.Collections.Generic;
using Fracture.Net.Serialization;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public sealed class ArraySerializerTests
    {
        #region Fields
        private readonly ArraySerialize serializer;
        #endregion
        
        public ArraySerializerTests()
        {
            var serializer = new ArraySerializer();
        }
        
        public void Serializes_To_Buffer_Correctly()
        {
        }

        public void Deserializes_To_Value_Correctly()
        {
        }
        
        public void Test_Size_From_Value()
        {
        }
        
        public void Test_Size_From_Buffer()
        {
        }
    }
}