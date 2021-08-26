using BenchmarkDotNet.Attributes;
using Fracture.Common.Memory;

namespace Fracture.Benchmarks.Serialization
{
    public class BenchmarkMemoryMapper
    {
        #region Constant fields
        private const int TestValue = 13379393;
        #endregion

        #region Fields
        private readonly byte[] buffer;
        #endregion
        
        public BenchmarkMemoryMapper()
            => buffer = new byte[128];
        
        [Benchmark]
        public void SerializeUsingMarshal()
            => MemoryMapper.Write(TestValue, buffer, 0);
        
        [Benchmark]
        public void SerializeUsingBitOperations()
            => MemoryMapper.WriteInt(TestValue, buffer, 0);
        
        [Benchmark]
        public void DeserializeUsingMarshal()
            => MemoryMapper.Read<int>(buffer, 0);
        
        [Benchmark]
        public void DeserializeUsingBitOperations()
            => MemoryMapper.ReadInt(buffer, 0);
    }
}