using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Fracture.Common.Memory;
using Fracture.Net;

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

    public class BenchmarkMemoryMapperGenericTypes
    {
        #region Constant fields
        private const int TestValue = 9995223;
        #endregion

        #region Fields
        private readonly byte[] buffer;
        #endregion
        
        public BenchmarkMemoryMapperGenericTypes()
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
    
    public class BenchmarkMemoryMapperArrayTypes
    {
        #region Constant fields
        private const int TestValue = 9995223;
        #endregion

        #region Fields
        private byte[] buffer;
        
        private int[] testArray;
        #endregion

        #region Properties
        [Params(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096)]
        // ReSharper disable once MemberCanBePrivate.Global - value injected by benchmark runner.
        public int ArraySize
        {
            get;
            // ReSharper disable once UnusedAutoPropertyAccessor.Global - value injected by benchmark.
            set;
        }
        #endregion
        
        public BenchmarkMemoryMapperArrayTypes()
        {
        }
        
        [GlobalSetup]
        public void GlobalSetup()
        {
            buffer    = new byte[sizeof(int) * ArraySize];
            testArray = new int[ArraySize];
            
            var offset = 0;
            
            for (var i = 0; i < testArray.Length; i++)
            {
                MemoryMapper.WriteInt(i, buffer, offset);
                
                testArray[i] = i;
                
                offset += sizeof(int);
            }
        }
        
        [Benchmark]
        public void SerializeUsingMarshal()
            => MemoryMapper.WriteArray(buffer, 0, testArray, 0, ArraySize);
        
        [Benchmark]
        public void SerializeUsingBitOperations()       
        {
            var offset = 0;
            
            for (var i = 0; i < ArraySize; i++)
            {
                MemoryMapper.WriteInt(i, buffer, offset);
                
                offset += sizeof(int);
            }
        }
        
        [Benchmark]
        public void DeserializeUsingMarshal()
            => MemoryMapper.ReadArray(testArray, 0, buffer, 0, ArraySize);
        
        [Benchmark]
        public void DeserializeUsingBitOperations()
        {
            var offset = 0;
            
            for (var i = 0; i < ArraySize; i++)
            {
                testArray[i] = MemoryMapper.ReadInt(buffer, offset);
                
                offset += sizeof(int);
            }
        }
    }
}