using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Kontent.Ai.Delivery.Benchmarks;

internal class BenchmarkConfig : ManualConfig
{
    private const string BenchmarkArtifactsFolder = "BenchmarkDotNet.Artifacts";

    internal BenchmarkConfig()
    {
        Add(DefaultConfig.Instance);

        var rootFolder = Environment.CurrentDirectory;
        var runFolder = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        ArtifactsPath = Path.Combine(rootFolder, BenchmarkArtifactsFolder, runFolder);

        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
