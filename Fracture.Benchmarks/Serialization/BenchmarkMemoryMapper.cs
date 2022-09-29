using BenchmarkDotNet.Attributes;

namespace Fracture.Benchmarks.Serialization
{
    public class MemoryMapper
    {
        #region Constant fields
        private const int TestValue = 13379393;
        #endregion

        #region Fields
        private readonly byte[] buffer;
        #endregion

        public MemoryMapper()
            => buffer = new byte[128];

        [Benchmark]
        public void SerializeUsingMarshal()
            => Common.Memory.MemoryMapper.Write(TestValue, buffer, 0);

        [Benchmark]
        public void SerializeUsingBitOperations()
            => Common.Memory.MemoryMapper.WriteInt(TestValue, buffer, 0);

        [Benchmark]
        public void DeserializeUsingMarshal()
            => Common.Memory.MemoryMapper.Read<int>(buffer, 0);

        [Benchmark]
        public void DeserializeUsingBitOperations()
            => Common.Memory.MemoryMapper.ReadInt(buffer, 0);
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
            => Common.Memory.MemoryMapper.Write(value, buffer, 0);

        private void SerializeUsingBitOperationsWithGenerics<T>(T value) where T : struct
            => Common.Memory.MemoryMapper.WriteInt((int)(object)value, buffer, 0);

        private T DeserializeUsingMarshalWithGenerics<T>() where T : struct
            => Common.Memory.MemoryMapper.Read<T>(buffer, 0);

        private T DeserializeUsingBitOperationsWithGenerics<T>() where T : struct
            => (T)(object)Common.Memory.MemoryMapper.ReadInt(buffer, 0);

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
        public void Setup()
        {
            buffer    = new byte[sizeof(int) * ArraySize];
            testArray = new int[ArraySize];

            var offset = 0;

            for (var i = 0; i < testArray.Length; i++)
            {
                Common.Memory.MemoryMapper.WriteInt(i, buffer, offset);

                testArray[i] = i;

                offset += sizeof(int);
            }
        }

        [Benchmark]
        public void Marshal_Serialize()
            => Common.Memory.MemoryMapper.WriteArray(buffer, 0, testArray, 0, ArraySize);

        [Benchmark]
        public void BitOperation_Serialize()
        {
            var offset = 0;

            for (var i = 0; i < ArraySize; i++)
            {
                Common.Memory.MemoryMapper.WriteInt(i, buffer, offset);

                offset += sizeof(int);
            }
        }

        [Benchmark]
        public void Marshal_Deserialize()
            => Common.Memory.MemoryMapper.ReadArray(testArray, 0, buffer, 0, ArraySize);

        [Benchmark]
        public void BitOperation_Deserialize()
        {
            var offset = 0;

            for (var i = 0; i < ArraySize; i++)
            {
                testArray[i] = Common.Memory.MemoryMapper.ReadInt(buffer, offset);

                offset += sizeof(int);
            }
        }
    }
}