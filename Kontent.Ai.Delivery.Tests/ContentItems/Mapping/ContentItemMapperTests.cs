using System.Text.Json;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Kontent.Ai.Delivery.Generated;
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

        var typeProvider = new GeneratedTypeProvider();
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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);
        Assert.All(item.Elements.RelatedArticles!, linked =>
        {
            Assert.NotNull(linked.System.Codename);
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
        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);
    }

    [Fact]
    public async Task MapElementsAsync_CircularReference_ReturnsSameInstance()
    {
        // Fixture has: on_roasts -> coffee_processing_techniques -> on_roasts (cycle)
        var json = await LoadFixtureAsync("onroast_recursive_linked_items.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        // Pre-register the main item in ItemsBeingHydrated to simulate
        // "on_roasts" being hydrated when its linked items are resolved
        context.ItemsBeingHydrated["on_roasts"] = item;

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);

        // Find coffee_processing_techniques in related articles
        var coffeeProcessing = item.Elements.RelatedArticles!
            .OfType<IContentItem<Article>>()
            .FirstOrDefault(a => a.System.Codename == "coffee_processing_techniques");

        Assert.NotNull(coffeeProcessing);
        Assert.NotNull(coffeeProcessing!.Elements.RelatedArticles);

        // Find on_roasts in coffee_processing_techniques' related articles
        var circularOnRoasts = coffeeProcessing.Elements.RelatedArticles!
            .OfType<IContentItem<Article>>()
            .FirstOrDefault(a => a.System.Codename == "on_roasts");

        Assert.NotNull(circularOnRoasts);

        // The circular reference should point to the SAME instance
        Assert.Same(item, circularOnRoasts);
    }

    [Fact]
    public async Task CompleteItemAsync_CircularReference_ReturnsSameRootInstance()
    {
        // Integration test: verify the full production flow via CompleteItemAsync
        // Fixture has: on_roasts -> coffee_processing_techniques -> on_roasts (cycle)
        var json = await LoadFixtureAsync("onroast_recursive_linked_items.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;

        // Use the production code path
        await _mapper.CompleteItemAsync(item, response.ModularContent);

        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.NotEmpty(item.Elements.RelatedArticles!);

        // Find coffee_processing_techniques in related articles
        var coffeeProcessing = item.Elements.RelatedArticles!
            .OfType<IContentItem<Article>>()
            .FirstOrDefault(a => a.System.Codename == "coffee_processing_techniques");

        Assert.NotNull(coffeeProcessing);
        Assert.NotNull(coffeeProcessing!.Elements.RelatedArticles);

        // Find on_roasts in coffee_processing_techniques' related articles
        var circularOnRoasts = coffeeProcessing.Elements.RelatedArticles!
            .OfType<IContentItem<Article>>()
            .FirstOrDefault(a => a.System.Codename == "on_roasts");

        Assert.NotNull(circularOnRoasts);

        // The circular reference should point to the SAME root instance
        Assert.Same(item, circularOnRoasts);
    }

    [Fact]
    public async Task MapElementsAsync_HandlesRecursiveInlineLinkedItems_WithoutStackOverflow()
    {
        // This fixture has on_roasts with body_copy rich text containing an inline reference to itself
        var json = await LoadFixtureAsync("onroast_recursive_inline_linked_items.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = response.ModularContent,
            CancellationToken = CancellationToken.None
        };

        // Should not throw StackOverflowException - cycle detection should break the recursion
        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        // Rich text content should be parsed despite self-reference
        Assert.NotNull(item.Elements.BodyCopy);
        Assert.NotEmpty(item.Elements.BodyCopy);

        // The embedded content item should be resolved (shallow due to cycle detection)
        var embeddedContent = item.Elements.BodyCopy.OfType<IEmbeddedContent>().FirstOrDefault();
        Assert.NotNull(embeddedContent);
        Assert.Equal("on_roasts", embeddedContent.System.Codename);
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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

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

        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

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
            _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context));
    }

    [Fact]
    public async Task MapElementsAsync_WithEmptyModularContent_GracefullyHandlesMissingLinkedItems()
    {
        // Arrange - simulates depth=0 where linked items are referenced but not in modular_content
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        // Create context with empty modular_content (simulating depth=0 response)
        var context = new MappingContext
        {
            ModularContent = new Dictionary<string, JsonElement>(), // Empty - items not resolved
            CancellationToken = CancellationToken.None
        };

        // Act - should not throw
        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        // Assert - linked items list should be empty (not null, not throwing)
        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.Empty(item.Elements.RelatedArticles!);
    }

    [Fact]
    public async Task MapElementsAsync_WithNullModularContent_GracefullyHandlesMissingLinkedItems()
    {
        // Arrange - null modular_content
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        var context = new MappingContext
        {
            ModularContent = null, // Null modular content
            CancellationToken = CancellationToken.None
        };

        // Act - should not throw
        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        // Assert - linked items list should be empty (not null, not throwing)
        Assert.NotNull(item.Elements.RelatedArticles);
        Assert.Empty(item.Elements.RelatedArticles!);
    }

    [Fact]
    public async Task MapElementsAsync_WithEmptyModularContent_GracefullyHandlesMissingEmbeddedContent()
    {
        // Arrange - fixture has embedded tweets in rich text, but modular_content is empty
        var json = await LoadFixtureAsync("coffee_beverages_explained.json");
        var response = JsonSerializer.Deserialize<DeliveryItemResponse<Article>>(json, _jsonOptions)!;
        var item = response.Item;
        var rawItem = (IRawContentItem)item;

        // Create context with empty modular_content (simulating depth=0 response)
        var context = new MappingContext
        {
            ModularContent = new Dictionary<string, JsonElement>(), // Empty - items not resolved
            CancellationToken = CancellationToken.None
        };

        // Act - should not throw
        await _mapper.MapElementsAsync(item.Elements, GetRawElements(rawItem), context);

        // Assert - rich text should have blocks, but no embedded content (they're filtered out)
        Assert.NotNull(item.Elements.BodyCopy);
        var embeddedContent = item.Elements.BodyCopy.OfType<IEmbeddedContent>().ToList();
        Assert.Empty(embeddedContent); // Missing items are gracefully omitted
    }

    private static async Task<string> LoadFixtureAsync(string filename)
    {
        var path = Path.Combine(
            Environment.CurrentDirectory,
            $"Fixtures{Path.DirectorySeparatorChar}DeliveryClient{Path.DirectorySeparatorChar}{filename}");
        return await File.ReadAllTextAsync(path);
    }

    /// <summary>
    /// Extracts the raw elements JsonElement from the full item JSON.
    /// </summary>
    private static JsonElement GetRawElements(IRawContentItem rawItem)
    {
        var rawItemJson = rawItem.RawItemJson!.Value;
        return rawItemJson.GetProperty("elements");
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
