using System;
using System.IO;
using Kontent.Ai.Delivery.Abstractions;
using BenchmarkDotNet.Attributes;
using RichardSzalay.MockHttp;
using System.Threading.Tasks;
using BenchmarkDotNet.Jobs;
using Kontent.Ai.Delivery;
using System.Collections.Generic;
using Kontent.Ai.Delivery.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Benchmarks.ContentTypes;

namespace Kontent.Ai.Delivery.Benchmarks;

public class DeliveryClientBenchmark
{
    private IDeliveryClient _client;

    private static IDeliveryClient CreateClient(MockHttpMessageHandler mockHttp, DeliveryOptions options)
    {
        var services = new ServiceCollection(); // TODO: cleanup

        // services.AddSingleton<IModelProvider>(sp =>
        // {
        //     var contentItemsProcessor = new InlineContentItemsProcessor();
        //     var customResolver = new DefaultContentLinkUrlResolver();
        //     var typeProvider = new CustomTypeProvider();
        //     var propertyMapper = new PropertyMapper();
        //     var htmlParser = new HtmlParser();
        //     var optionsMonitor = DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { EnvironmentId = guid });
        //     // Use the same JSON options as Refit
        //     var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        //     return new ModelProvider(typeProvider, propertyMapper, contentItemsProcessor, customResolver, jsonOptions, htmlParser, optionsMonitor);
        // });

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
            .WithQueryString("system.type=article")
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
    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<Article>>>> GetItemsAsync()
    {
        return await _client.GetItems<Article>().Filter(filter => filter.Equals(ItemSystemPath.Type, "article")).ExecuteAsync();
    }
}
