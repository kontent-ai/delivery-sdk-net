using System.Linq;
using BenchmarkDotNet.Running;
using Xunit;

namespace Kentico.Kontent.Delivery.Benchmarks
{
    public class Harness
    {
        [Fact]
        public void DeliveryClient() => RunBenchmark<DeliveryClientBenchmark>();

        /// <remarks>
        /// Load all benchmarks to <see cref="Benchmarks.All"/> and make them acessible from the command line (<see cref="Program.Main"/>).        
        /// </remarks>
        private static void RunBenchmark<TBenchmark>()
        {
            var targetType = typeof(TBenchmark);
            var benchmarkType = Benchmarks.All.Single(type => type == targetType);
            BenchmarkRunner.Run(benchmarkType, new BenchmarkConfig());
        }
    }
}
