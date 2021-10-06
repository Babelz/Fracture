using System.Collections.Generic;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation.Builders;
using Xunit;

namespace Fracture.Net.Tests.Serialization
{
    [Trait("Category", "Serialization")]
    public class DictionarySerializerTests
    {
        public DictionarySerializerTests()
        {
            ObjectSerializationSchema.DefineDictionary(typeof(Dictionary<string, int>));
            ObjectSerializationSchema.DefineDictionary(typeof(Dictionary<string, int?>));
        }
        
        [Fact]
        public void Serialization_Back_And_Forth_Works_With_Non_Nullable_Primitive_Types()
        {
            var lookupIn = new Dictionary<string, int>()
            {
                { "a", 200 },
                { "b", 400 },
                { "c", 800 }
            };
            
            var buffer = new byte[128];
            
            DictionarySerializer.Serialize(lookupIn, buffer, 0);
            
            var lookupOut = DictionarySerializer.Deserialize<string, int>(buffer, 0);
            
            Assert.Equal(lookupIn.Count, lookupOut.Count);
            
            foreach (var key in lookupIn.Keys)
            {
                Assert.True(lookupOut.ContainsKey(key));
                Assert.Equal(lookupIn[key], lookupOut[key]);
            }
        }
        
        [Fact]
        public void Serialization_Back_And_Forth_Works_With_Nullable_Primitive_Types()
        {
            var lookupIn = new Dictionary<string, int?>()
            {
                { "a", 200 },
                { "b", 400 },
                { "c", null },
                { "d", 1600 }
            };
            
            var buffer = new byte[128];
            
            DictionarySerializer.Serialize(lookupIn, buffer, 0);
            
            var lookupOut = DictionarySerializer.Deserialize<string, int?>(buffer, 0);
            
            Assert.Equal(lookupIn.Count, lookupOut.Count);
            
            foreach (var key in lookupIn.Keys)
            {
                Assert.True(lookupOut.ContainsKey(key));
                Assert.Equal(lookupIn[key], lookupOut[key]);
            }
        }
    }
}