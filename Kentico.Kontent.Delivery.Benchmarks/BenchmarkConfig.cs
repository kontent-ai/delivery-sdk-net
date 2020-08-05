using System;
using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Kentico.Kontent.Delivery.Benchmarks
{
    internal class BenchmarkConfig : ManualConfig
    {
        private const string BenchmarkArtifactsFolder = "BenchmarkDotNet.Artifacts";

        internal BenchmarkConfig()
        {
            Add(DefaultConfig.Instance);

            var rootFolder = AppContext.BaseDirectory;
            var runFolder = DateTime.UtcNow.ToString("dd-MM-yyyy_hh-MM-ss");
            ArtifactsPath = Path.Combine(rootFolder, BenchmarkArtifactsFolder, runFolder);

            //TODO: replace when fixed https://github.com/dotnet/BenchmarkDotNet/issues/1452
            Add(MemoryDiagnoser.Default);
            //AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}
