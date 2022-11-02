using BenchmarkDotNet.Running;
using Fracture.Benchmarks.Serialization;

namespace Fracture.Benchmarks
{
    public static class Program
    {
        private static void Main(string[] args)
            => BenchmarkRunner.Run<BenchmarkMemoryMapperArrayTypes>();
    }
}