using BenchmarkDotNet.Running;
using Storage.Benchmarks;

namespace Storage.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SerializationBenchmarks>();
        }
    }
}
