using BenchmarkDotNet.Running;
using Fracture.Benchmarks.Tests;

namespace Fracture.Benchmarks
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<TestDynamicSerializeDelegate>();
        }
    }
}