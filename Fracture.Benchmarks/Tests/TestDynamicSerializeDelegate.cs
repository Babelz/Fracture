using System.Linq;
using BenchmarkDotNet.Attributes;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Benchmarks.Tests
{
    public sealed class TestClass
    {
        #region Fields
        public int? X
        {
            get;
            set;
        }
        public int? Y
        {
            get;
            set;
        }

        public int I
        {
            get;
            set;
        }
        public int J
        {
            get;
            set;
        }
        
        public string S1
        {
            get;
            set;
        }
        
        public string S2
        {
            get;
            set;
        }
        #endregion

        public TestClass()
        {
        }
    }
    
    public sealed class FooSerializer
    {
        public FooSerializer()
        {
        }
        
        public void Serialize(in ObjectSerializationValueRanges valueRanges, object value, byte[] buffer, int offset)
        {
            var actual = (TestClass)value;
            var currentSerializer = valueRanges.ValueSerializerTypes[0];
            var nullMask = new BitField(1);
            var nullMaskOffset = 0;
            var bitFieldSerializer = valueRanges.BitFieldSerializer;
            
            offset += 1;
            
            if (actual.X.HasValue)
            {
                currentSerializer.Serialize(actual.X.Value, buffer, offset);
                offset += currentSerializer.GetSizeFromValue(actual.X.Value);
            }
            else
            {
                nullMask.SetBit(0, true);
            }
            
            currentSerializer = valueRanges.ValueSerializerTypes[1];
            
            if (actual.Y.HasValue)
            {
                currentSerializer.Serialize(actual.Y.Value, buffer, offset);
                offset += currentSerializer.GetSizeFromValue(actual.Y.Value);
            }
            else
            {
                nullMask.SetBit(1, true);
            }
            
            currentSerializer = valueRanges.ValueSerializerTypes[2];
            
            currentSerializer.Serialize(actual.I, buffer, offset);
            offset += currentSerializer.GetSizeFromValue(actual.I);
            
            currentSerializer = valueRanges.ValueSerializerTypes[3];
            
            currentSerializer.Serialize(actual.J, buffer, offset);
            offset += currentSerializer.GetSizeFromValue(actual.J);
            
            currentSerializer = valueRanges.ValueSerializerTypes[4];
            
            if (actual.S1 != null)
            {
                currentSerializer.Serialize(actual.S1, buffer, offset);
                offset += currentSerializer.GetSizeFromValue(actual.S1);
            }
            else
            {
                nullMask.SetBit(2, true);
            }
            
            currentSerializer = valueRanges.ValueSerializerTypes[5];
            
            if (actual.S2 != null)
            {
                currentSerializer.Serialize(actual.S2, buffer, offset);
            }
            else
            {
                nullMask.SetBit(3, true);
            }
            
            bitFieldSerializer.Serialize(nullMask, buffer, nullMaskOffset);
        }
    }
    
    public class TestDynamicSerializeDelegate
    {
        #region Fields
        private readonly TestClass testObject;
        
        private readonly ObjectSerializationValueRanges valueRanges;
        private readonly DynamicSerializeDelegate serializeDelegate;
        private readonly byte[] buffer;
        
        private readonly FooSerializer serializer;
        #endregion
        
        public TestDynamicSerializeDelegate()
        {
            var mapping = ObjectSerializationMapper.Create()
                                                   .FromType<TestClass>()
                                                   .PublicFields()
                                                   .PublicProperties()
                                                   .Map();
            
            var serializationOps = ObjectSerializerCompiler.CompileSerializationOps(mapping).ToList().AsReadOnly();
            
            testObject = new TestClass()
            {
                X = 1500, 
                Y = null,
                I = 200,
                J = 300,
                S1 = null,
                S2 = null
            };
            
            serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(typeof(TestClass), serializationOps, 4);
            buffer            = new byte[256];

            valueRanges = ObjectSerializerInterpreter.InterpretObjectSerializationNullContext(
                typeof(TestClass),
                serializationOps, 
                ObjectSerializerProgram.GetOpValueSerializerTypes(serializationOps).ToList().AsReadOnly()
            );
            
            serializer = new FooSerializer();
        }

        [Benchmark]
        public void SerializeWithDynamicDelegate()
        {            
            serializeDelegate(valueRanges, testObject, buffer, 0);
        }
        
        [Benchmark]
        public void SerializeWithInstanceCall()
        {
            serializer.Serialize(valueRanges, testObject, buffer, 0);
        }
    }
}