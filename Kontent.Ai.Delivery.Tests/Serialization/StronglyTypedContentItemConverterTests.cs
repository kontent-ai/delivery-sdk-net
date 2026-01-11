using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Serialization.Converters;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Serialization;

public class StronglyTypedContentItemConverterTests
{
    /// <summary>
    /// Simple test model with various element types for testing deserialization.
    /// </summary>
    private record SimpleTestModel
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("price")]
        public decimal? Price { get; init; }

        [JsonPropertyName("count")]
        public int? Count { get; init; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; init; }
    }

    private const string SimpleArticleJson = """
        {
            "system": {
                "id": "cf106f4e-30a4-42ef-b313-b8ea3fd3e5c5",
                "name": "Test Article",
                "codename": "test_article",
                "language": "en-US",
                "type": "article",
                "collection": "default",
                "sitemap_locations": [],
                "last_modified": "2024-01-01T00:00:00Z",
                "workflow": "default",
                "workflow_step": "published"
            },
            "elements": {
                "title": {
                    "type": "text",
                    "name": "Title",
                    "value": "Test Article Title"
                },
                "summary": {
                    "type": "text",
                    "name": "Summary",
                    "value": "Test summary content"
                },
                "post_date": {
                    "type": "date_time",
                    "name": "Post date",
                    "value": "2024-01-15T10:30:00Z"
                }
            }
        }
        """;

    private const string SimpleTestModelJson = """
        {
            "system": {
                "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
                "name": "Test Item",
                "codename": "test_item",
                "language": "en-US",
                "type": "test_type",
                "collection": "default",
                "sitemap_locations": [],
                "last_modified": "2024-01-01T00:00:00Z",
                "workflow": "default",
                "workflow_step": "published"
            },
            "elements": {
                "title": {
                    "type": "text",
                    "name": "Title",
                    "value": "Test Title"
                },
                "price": {
                    "type": "number",
                    "name": "Price",
                    "value": 19.99
                },
                "count": {
                    "type": "number",
                    "name": "Count",
                    "value": 42
                },
                "is_active": {
                    "type": "custom",
                    "name": "Is Active",
                    "value": true
                }
            }
        }
        """;

    [Fact]
    public void Deserialize_WithSimpleTextElements_DeserializesCorrectly()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.System);
        Assert.Equal("test_article", result.System.Codename);
        Assert.Equal("Test Article Title", result.Elements.Title);
        Assert.Equal("Test summary content", result.Elements.Summary);
    }

    [Fact]
    public void Deserialize_WithDateTimeElement_LeavesNullForHydration()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert - DateTime is a complex type, so it should be null after initial deserialization
        // The ContentItemMapper will hydrate it later with the full IDateTimeContent object
        Assert.NotNull(result);
        Assert.Null(result.Elements.PostDate);
    }

    [Fact]
    public void Deserialize_WithNumberElement_DeserializesCorrectly()
    {
        // Arrange
        var options = CreateOptionsWithConverter<SimpleTestModel>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<SimpleTestModel>>(SimpleTestModelJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Elements.Title);
        Assert.Equal(19.99m, result.Elements.Price);
        Assert.Equal(42, result.Elements.Count);
        Assert.True(result.Elements.IsActive);
    }

    [Fact]
    public void Deserialize_MultipleTimes_CachesJsonSerializerOptions()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();
        var cacheField = typeof(StronglyTypedContentItemConverter<Article>)
            .GetField("OptionsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(cacheField);

        var cache = cacheField.GetValue(null) as ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions>;
        Assert.NotNull(cache);

        // Clear any existing cache entries by creating fresh options
        var freshOptions = CreateOptionsWithConverter<Article>();

        // Act - deserialize multiple times with the same options
        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, freshOptions);
        var cacheHit1 = cache.TryGetValue(freshOptions, out var cached1);

        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, freshOptions);
        var cacheHit2 = cache.TryGetValue(freshOptions, out var cached2);

        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, freshOptions);
        var cacheHit3 = cache.TryGetValue(freshOptions, out var cached3);

        // Assert - all deserializations should use the same cached options
        Assert.True(cacheHit1, "First deserialization should populate the cache");
        Assert.True(cacheHit2, "Second deserialization should hit the cache");
        Assert.True(cacheHit3, "Third deserialization should hit the cache");
        Assert.Same(cached1, cached2);
        Assert.Same(cached2, cached3);
    }

    [Fact]
    public void Deserialize_WithDifferentOptionsInstances_CreatesSeparateCacheEntries()
    {
        // Arrange
        var options1 = CreateOptionsWithConverter<Article>();
        var options2 = CreateOptionsWithConverter<Article>();

        var cacheField = typeof(StronglyTypedContentItemConverter<Article>)
            .GetField("OptionsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var cache = cacheField?.GetValue(null) as ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions>;
        Assert.NotNull(cache);

        // Act
        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options1);
        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options2);

        // Assert
        Assert.True(cache.TryGetValue(options1, out var cached1));
        Assert.True(cache.TryGetValue(options2, out var cached2));
        Assert.NotSame(cached1, cached2); // Different source options = different cached options
    }

    [Fact]
    public async Task Deserialize_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();
        var tasks = new List<Task<ContentItem<Article>?>>();
        const int concurrentCalls = 100;

        // Act - perform many concurrent deserializations
        for (int i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() =>
                JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all deserializations should succeed
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Equal("Test Article Title", result.Elements.Title);
        });
    }

    [Fact]
    public void Deserialize_CachedOptions_DoesNotContainContentItemConverterFactory()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();
        var cacheField = typeof(StronglyTypedContentItemConverter<Article>)
            .GetField("OptionsCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var cache = cacheField?.GetValue(null) as ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions>;
        Assert.NotNull(cache);

        // Act
        _ = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert
        Assert.True(cache.TryGetValue(options, out var cachedOptions));
        Assert.NotNull(cachedOptions);

        // The cached options should NOT contain ContentItemConverterFactory
        Assert.DoesNotContain(cachedOptions.Converters, c => c is ContentItemConverterFactory);

        // But the original options SHOULD still contain it
        Assert.Contains(options.Converters, c => c is ContentItemConverterFactory);
    }

    [Fact]
    public void Deserialize_PreservesRawElementsForPostProcessing()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RawElements);
        Assert.Equal(JsonValueKind.Object, result.RawElements.Value.ValueKind);

        // Raw elements should contain the original structure
        Assert.True(result.RawElements.Value.TryGetProperty("title", out var titleElement));
        Assert.True(titleElement.TryGetProperty("type", out var typeValue));
        Assert.Equal("text", typeValue.GetString());
    }

    private static JsonSerializerOptions CreateOptionsWithConverter<T>()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new ContentItemConverterFactory());
        return options;
    }
}
