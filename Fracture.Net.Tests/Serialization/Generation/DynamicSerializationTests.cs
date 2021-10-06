using System.Collections.Generic;
using System.Linq;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;
using Newtonsoft.Json;
using Xunit;

namespace Fracture.Net.Tests.Serialization.Generation
{
    [Trait("Category", "Serialization")]
    public class DynamicSerializationTests
    {
        #region Test types
        private sealed class AllFieldTypesTestClass
        {
            #region Fields
            public int X
            {
                get;
                set;
            }
            public int? MaybeX
            {
                get;
                set;
            }
            
            public string S
            {
                get;
                set;
            }
            public string? MaybeS
            {
                get;
                set;
            }
            
            public int[] Numbers
            {
                get;
                set;
            }
            public int?[] NullableNumbers
            {
                get;
                set;
            }
            public int[]? MaybeNumbers
            {
                get;
                set;
            }
            
            public Dictionary<string, float> Map
            {
                get;
                set;
            }
            public Dictionary<int, string?> NullableMap
            {
                get;
                set;
            }
            public Dictionary<int, int>? MaybeMap
            {
                get;
                set;
            }
            #endregion
        }
        #endregion

        static DynamicSerializationTests()
        {
            ObjectSerializationSchema.DefineArray(typeof(string[]));
            
            ObjectSerializationSchema.DefineNullable(typeof(int?));
            
            ObjectSerializationSchema.DefineDictionary(typeof(Dictionary<int, int>));
            ObjectSerializationSchema.DefineDictionary(typeof(Dictionary<string, float>));
            ObjectSerializationSchema.DefineDictionary(typeof(Dictionary<int, string?>));
        }
        
        public DynamicSerializationTests()
        {
        }
        
        [Fact()]
        public void Serialization_Back_And_Forth_Works_With_All_Field_Types()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<AllFieldTypesTestClass>()
                                                   .PublicFields()
                                                   .PublicProperties()
                                                   .Map();
            
            var deserializationOps = ObjectSerializerCompiler.CompileDeserializationOps(mapping).ToList().AsReadOnly();
            var serializationOps   = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();

            var deserializationValueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(AllFieldTypesTestClass), deserializationOps);
            var serializationValueRanges   = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(AllFieldTypesTestClass), serializationOps);

            var deserializeDelegate = ObjectSerializerInterpreter.InterpretDynamicDeserializeDelegate(
                deserializationValueRanges,
                typeof(AllFieldTypesTestClass), 
                deserializationOps
            );
            
            var serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(
                serializationValueRanges,
                typeof(AllFieldTypesTestClass),
                serializationOps
            );
            
            var testObjectIn = new AllFieldTypesTestClass()
            {
                X      = 200,
                MaybeX = null,
                
                S      = "hello world!",
                MaybeS = "this should not be null",

                Numbers         = new [] { 200, 300, 400, 500 },
                NullableNumbers = new int?[] { null, null, 200, null, null },
                MaybeNumbers    = null,
                
                Map = new Dictionary<string, float>()
                {
                    { "x", 200.0f },
                    { "y", 500.0f },
                    { "z", 5.0f },
                },
                MaybeMap = new Dictionary<int, int>()
                {
                    { 10, 0 },
                    { 20, 1 },
                    { 30, 2 },
                },
                NullableMap = new Dictionary<int, string?>()
                {
                    { 1, null },
                    { 2, null },
                    { 3, "hello!" },
                }
            };

            var buffer = new byte[256];
            
            serializeDelegate(testObjectIn, buffer, 0);
            
            var testObjectOut = (AllFieldTypesTestClass)deserializeDelegate(buffer, 0);
            
            // This is bit hacky but least painful way i came up quickly to check for object equality without writing custom comparer. 
            Assert.Equal(JsonConvert.SerializeObject(testObjectIn), JsonConvert.SerializeObject(testObjectOut)); 
        }
    }
}