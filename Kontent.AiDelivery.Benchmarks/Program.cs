using BenchmarkDotNet.Running;

namespace Kentico.Kontent.Delivery.Benchmarks
{
    internal static class Program
    {
        internal static void Main(string[] args) =>
            new BenchmarkSwitcher(Benchmarks.All).Run(args, new BenchmarkConfig());
    }
}

