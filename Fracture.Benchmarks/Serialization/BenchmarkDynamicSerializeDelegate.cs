using System.Linq;
using BenchmarkDotNet.Attributes;
using Fracture.Net.Serialization;
using Fracture.Net.Serialization.Generation;
using Fracture.Net.Serialization.Generation.Builders;

namespace Fracture.Benchmarks.Serialization
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
    
    public sealed class TestClassSerializer
    {
        public TestClassSerializer()
        {
        }
        
        public void Serialize(in ObjectSerializationValueRanges valueRanges, object value, byte[] buffer, int offset)
        {
            var actual = (TestClass)value;
            var nullMask = new BitField(1);
            var nullMaskOffset = 0;
            
            offset += 1;
            
            if (actual.X.HasValue)
            {
                IntSerializer.Serialize(actual.X.Value, buffer, offset);
                offset += IntSerializer.GetSizeFromValue(actual.X.Value);
            }
            else
            {
                nullMask.SetBit(0, true);
            }
            
            if (actual.Y.HasValue)
            {
                IntSerializer.Serialize(actual.Y.Value, buffer, offset);
                offset += IntSerializer.GetSizeFromValue(actual.Y.Value);
            }
            else
            {
                nullMask.SetBit(1, true);
            }
            
            IntSerializer.Serialize(actual.I, buffer, offset);
            offset += IntSerializer.GetSizeFromValue(actual.I);
            
            IntSerializer.Serialize(actual.J, buffer, offset);
            offset += IntSerializer.GetSizeFromValue(actual.J);
            
            if (actual.S1 != null)
            {
                StringSerializer.Serialize(actual.S1, buffer, offset);
                offset += StringSerializer.GetSizeFromValue(actual.S1);
            }
            else
            {
                nullMask.SetBit(2, true);
            }
            
            if (actual.S2 != null)
            {
                StringSerializer.Serialize(actual.S2, buffer, offset);
            }
            else
            {
                nullMask.SetBit(3, true);
            }
            
            BitFieldSerializer.Serialize(nullMask, buffer, nullMaskOffset);
        }
    }
    
    public class BenchmarkDynamicSerializeDelegate
    {
        #region Fields
        private readonly TestClass testObject;
        
        private readonly ObjectSerializationValueRanges valueRanges;
        private readonly DynamicSerializeDelegate serializeDelegate;
        private readonly byte[] buffer;
        
        private readonly TestClassSerializer serializer;
        #endregion
        
        public BenchmarkDynamicSerializeDelegate()
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
            
            valueRanges       = ObjectSerializerInterpreter.InterpretObjectSerializationValueRanges(typeof(TestClass), serializationOps);
            serializeDelegate = ObjectSerializerInterpreter.InterpretDynamicSerializeDelegate(in valueRanges, typeof(TestClass), serializationOps);
            buffer            = new byte[256];

            serializer = new TestClassSerializer();
        }

        [Benchmark]
        public void SerializeWithDynamicDelegate()
            => serializeDelegate(testObject, buffer, 0);
        
        [Benchmark]
        public void SerializeWithInstanceCall()
            => serializer.Serialize(valueRanges, testObject, buffer, 0);
    }
}