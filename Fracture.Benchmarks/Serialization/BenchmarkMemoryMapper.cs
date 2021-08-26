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
        
        private void SerializeUsingMarshalWithGenerics<T>(T value) where T : struct
            => MemoryMapper.Write(value, buffer, 0);
        
        private void SerializeUsingBitOperationsWithGenerics<T>(T value) where T : struct
            => MemoryMapper.WriteInt((int)(object)value, buffer, 0);
        
        private T DeserializeUsingMarshalWithGenerics<T>() where T : struct
            => MemoryMapper.Read<T>(buffer, 0);
        
        private T DeserializeUsingBitOperationsWithGenerics<T>() where T : struct
            => (T)(object)MemoryMapper.ReadInt(buffer, 0);
        
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
        
        [Benchmark]
        public void SerializeUsingMarshalWithGenerics()
            => SerializeUsingMarshalWithGenerics(TestValue);
        
        [Benchmark]
        public void SerializeUsingBitOperationsWithGenerics()
            => SerializeUsingBitOperationsWithGenerics(TestValue);
        
        [Benchmark]
        public void DeserializeUsingMarshalWithGenerics()
            => DeserializeUsingMarshalWithGenerics<int>();
        
        [Benchmark]
        public void DeserializeUsingBitOperationsWithGenerics()
            => DeserializeUsingBitOperationsWithGenerics<int>();
    }
}