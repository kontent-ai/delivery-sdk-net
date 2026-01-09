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

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class ContentItemMapperTests
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ContentItemMapper _mapper;

    public ContentItemMapperTests()
    {
        _jsonOptions = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();

        var typeProvider = new CustomTypeProvider();
        var typingStrategy = new DefaultItemTypingStrategy(typeProvider);
        var deserializer = new ContentDeserializer(_jsonOptions);
        var htmlParser = new HtmlParser();
        var dependencyExtractor = new ContentDependencyExtractor();
        var optionsMonitor = new StaticOptionsMonitor<DeliveryOptions>(new DeliveryOptions());

        _mapper = new ContentItemMapper(
            typingStrategy,
            deserializer,
            htmlParser,
            optionsMonitor,
            dependencyExtractor);
    }

    [Fact]
    public async Task MapElementsAsync_MapsAssetElements()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        Assert.NotNull(item.Elements.TeaserImage);
        Assert.NotEmpty(item.Elements.TeaserImage!);
        Assert.All(item.Elements.TeaserImage!, asset => Assert.NotNull(asset.Url));
    }

    [Fact]
    public async Task MapElementsAsync_MapsTaxonomyElements()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        Assert.NotNull(item.Elements.Personas);
        Assert.NotEmpty(item.Elements.Personas!);
        Assert.All(item.Elements.Personas!, term =>
        {
            Assert.NotNull(term.Codename);
            Assert.NotNull(term.Name);
        });
    }

    [Fact]
    public async Task MapElementsAsync_MapsRichTextContent()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        Assert.NotNull(item.Elements.BodyCopy);
        Assert.NotEmpty(item.Elements.BodyCopy);
    }

    [Fact]
    public async Task MapElementsAsync_MapsLinkedItems()
    {
        // Use on_roasts.json which has actual related_articles values
        var json = await LoadFixtureAsync("on_roasts.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);
        Assert.All(item.Elements.RelatedArticles!, linked =>
        {
            Assert.NotNull(linked.Codename);
            Assert.NotEqual("unknown", linked.Codename);
        });
    }

    [Fact]
    public async Task MapElementsAsync_HandlesRecursiveLinkedItems_WithoutStackOverflow()
    {
        var json = await LoadFixtureAsync("onroast_recursive_linked_items.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        // Should not throw StackOverflowException
        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);
    }

    [Fact]
    public async Task MapElementsAsync_TracksAssetDependencies()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var dependencyContext = new DependencyTrackingContext();
        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            DependencyContext = dependencyContext,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        // Asset element payload extracts the asset GUID from the URL path
        var expectedAssetId = Guid.Parse("e700596b-03b0-4cee-ac5c-9212762c027a");
        Assert.Contains($"asset_{expectedAssetId}", dependencyContext.Dependencies);
    }

    [Fact]
    public async Task MapElementsAsync_TracksLinkedItemDependencies()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var dependencyContext = new DependencyTrackingContext();
        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            DependencyContext = dependencyContext,
            CancellationToken = CancellationToken.None
        };

        await _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context);

        // Should track linked items as dependencies
        Assert.Contains(dependencyContext.Dependencies, d => d.StartsWith("item_"));
    }

    [Fact]
    public async Task MapElementsAsync_RespectsCancellation()
    {
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = cts.Token
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _mapper.MapElementsAsync(item.Elements, rawItem.RawElements!.Value, context));
    }

    private static async Task<string> LoadFixtureAsync(string filename)
    {
        var path = Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{filename}");
        return await File.ReadAllTextAsync(path);
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
