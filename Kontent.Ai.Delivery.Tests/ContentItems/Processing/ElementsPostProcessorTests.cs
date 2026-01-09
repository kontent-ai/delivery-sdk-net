using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Processing;

public sealed class ElementsPostProcessorTests
{
    [Fact]
    public async Task ProcessAsync_TracksAssetDependencies_FromAssetElements_ByUrlSegment()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}coffee_beverages_explained.json"));

        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, jsonOptions)!;

        var postProcessor = CreatePostProcessor(jsonOptions);
        var dependencyContext = new DependencyTrackingContext();

        await postProcessor.ProcessAsync(response.Item, response.ModularContent, dependencyContext);

        // Asset element payload doesn't include explicit asset IDs; SDK extracts the asset GUID from the URL path.
        // https://assets.kontent.ai/{environmentId}/{assetId}/{filename}
        var expectedAssetId = Guid.Parse("e700596b-03b0-4cee-ac5c-9212762c027a");
        Assert.Contains($"asset_{expectedAssetId}", dependencyContext.Dependencies);
    }

    [Fact]
    public async Task ProcessAsync_HandlesRecursiveLinkedItems_WithoutStackOverflow()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}onroast_recursive_linked_items.json"));

        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, jsonOptions)!;

        var postProcessor = CreatePostProcessor(jsonOptions);

        await postProcessor.ProcessAsync(response.Item, response.ModularContent, dependencyContext: null);

        // Sanity-check: linked items element is hydrated to embedded content list
        Assert.NotNull(response.Item.Elements.RelatedArticles);
        Assert.NotEmpty(response.Item.Elements.RelatedArticles!);
    }

    [Fact]
    public async Task ProcessAsync_MapsLinkedItems_WithCorrectCodenames()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json"));

        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, jsonOptions)!;

        var postProcessor = CreatePostProcessor(jsonOptions);
        await postProcessor.ProcessAsync(response.Item, response.ModularContent, dependencyContext: null);

        Assert.NotNull(response.Item.Elements.RelatedArticles);
        Assert.NotEmpty(response.Item.Elements.RelatedArticles!);

        var codenames = response.Item.Elements.RelatedArticles!.Select(x => x.Codename).ToList();
        Assert.Contains("coffee_processing_techniques", codenames);
    }

    [Fact]
    public async Task ProcessAsync_TracksDependencies_ForLinkedItems()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}on_roasts.json"));

        var jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, jsonOptions)!;

        var postProcessor = CreatePostProcessor(jsonOptions);
        var dependencyContext = new DependencyTrackingContext();

        await postProcessor.ProcessAsync(response.Item, response.ModularContent, dependencyContext);

        // Verify linked items are tracked
        Assert.Contains("item_coffee_processing_techniques", dependencyContext.Dependencies);
    }

    private static IElementsPostProcessor CreatePostProcessor(JsonSerializerOptions jsonOptions)
    {
        var typeProvider = new CustomTypeProvider();
        var typingStrategy = new DefaultItemTypingStrategy(typeProvider);
        var deserializer = new ContentDeserializer(jsonOptions);
        var htmlParser = new HtmlParser();
        var dependencyExtractor = new ContentDependencyExtractor();
        var optionsMonitor = new StaticOptionsMonitor<DeliveryOptions>(new DeliveryOptions());

        var mapper = new ContentItemMapper(
            typingStrategy,
            deserializer,
            htmlParser,
            optionsMonitor,
            dependencyExtractor);

        return new ElementsPostProcessor(mapper);
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
