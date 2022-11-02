using System.Collections.Generic;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public class ListSerializerTests
    {
        static ListSerializerTests()
            => ObjectSerializerAnalyzer.Analyze(new[] { typeof(List<int>), typeof(List<int?>) });

        public ListSerializerTests()
        {
        }

        [Fact]
        public void Serialization_Back_And_Forth_Works_With_Non_Nullable_Primitive_Types()
        {
            var numbersIn = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
            };

            var buffer = new byte[128];

            ListSerializer.Serialize(numbersIn, buffer, 0);

            var numbersOut = ListSerializer.Deserialize<int?>(buffer, 0);

            Assert.Equal(numbersIn.Count, numbersOut.Count);

            for (var i = 0; i < numbersIn.Count; i++)
                Assert.Equal(numbersIn[i], numbersOut[i]);
        }

        [Fact]
        public void Serialization_Back_And_Forth_Works_With_Nullable_Primitive_Types()
        {
            var numbersIn = new List<int?>
            {
                0,
                1,
                2,
                null,
                4,
                null,
                null,
                7,
            };

            var buffer = new byte[128];

            ListSerializer.Serialize(numbersIn, buffer, 0);

            var numbersOut = ListSerializer.Deserialize<int?>(buffer, 0);

            Assert.Equal(numbersIn.Count, numbersOut.Count);

            for (var i = 0; i < numbersIn.Count; i++)
                Assert.Equal(numbersIn[i], numbersOut[i]);
        }
    }
}