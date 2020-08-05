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
        /// This method is used to enforce that benchmark types are added to <see cref="Benchmarks.All"/>
        /// so that they can be used directly from the command line in <see cref="Program.Main"/> as well.
        /// </remarks>
        private static void RunBenchmark<TBenchmark>()
        {
            var targetType = typeof(TBenchmark);
            var benchmarkType = Benchmarks.All.Single(type => type == targetType);
            BenchmarkRunner.Run(benchmarkType, new BenchmarkConfig());
        }
    }
}
