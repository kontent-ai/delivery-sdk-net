using Kontent.Ai.Delivery.Abstractions;
using BenchmarkDotNet.Attributes;
using RichardSzalay.MockHttp;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Kontent.Ai.Delivery.Benchmarks.ContentTypes;

namespace Kontent.Ai.Delivery.Benchmarks;

public class DeliveryClientBenchmark
{
    private IDeliveryClient _client = null!; // Initialized in GlobalSetup

    private static IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp, DeliveryOptions options)
    {
        var services = new ServiceCollection();

        services.AddDeliveryClient(options, configureHttpClient: b => b.ConfigurePrimaryHttpMessageHandler(() => mockHttp));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryClient>();
    }

    [GlobalSetup]
    public async Task Setup()
    {
        var environmentId = Guid.NewGuid();
        var baseUrl = $"https://deliver.kontent.ai/{environmentId}";
        var mockHttp = new MockHttpMessageHandler();

        mockHttp
            .When($"{baseUrl}/items/on_roasts")
            .Respond("application/json", await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}on_roasts.json")));

        mockHttp
            .When($"{baseUrl}/items")
            .WithQueryString("system.type[eq]", "article")
            .Respond("application/json",
                await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, $"Fixtures{Path.DirectorySeparatorChar}full_articles.json")));

        var _deliveryOptions = DeliveryOptionsBuilder.CreateInstance().WithEnvironmentId(environmentId).UseProductionApi().Build();
        _client = CreateClient(mockHttp, _deliveryOptions);
    }

    [Benchmark]
    public async Task<IDeliveryResult<IContentItem<Article>>> GetItemAsync()
    {
        return await _client.GetItem<Article>("on_roasts").ExecuteAsync();
    }

    [Benchmark]
    public async Task<IDeliveryResult<IDeliveryItemListingResponse<Article>>> GetItemsAsync()
    {
        return await _client.GetItems<Article>().Where(f => f.System("type").IsEqualTo("article")).ExecuteAsync();
    }
}
