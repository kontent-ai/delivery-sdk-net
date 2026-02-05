using System.Text.Json;
using System.Text.Json.Serialization;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Serialization.Converters;
using Kontent.Ai.Delivery.Tests.Models.ContentTypes;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Serialization;

/// <summary>
/// Tests for StronglyTypedContentItemConverter.
///
/// Note: The converter creates an empty model instance with system attributes
/// and RawItemJson preserved. All property values (simple and complex) are
/// populated later by ContentItemMapper.CompleteItemAsync().
/// </summary>
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
    public void Deserialize_ParsesSystemAttributes_Correctly()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.System);
        Assert.Equal("test_article", result.System.Codename);
        Assert.Equal("Test Article", result.System.Name);
        Assert.Equal("article", result.System.Type);
    }

    [Fact]
    public void Deserialize_CreatesEmptyElementsInstance()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert - Elements instance is created but properties are null
        // (ContentItemMapper will populate them during CompleteItemAsync)
        Assert.NotNull(result);
        Assert.NotNull(result.Elements);
        Assert.Null(result.Elements.Title);
        Assert.Null(result.Elements.Summary);
    }

    [Fact]
    public void Deserialize_WithAllElementTypes_CreatesEmptyElementsInstance()
    {
        // Arrange
        var options = CreateOptionsWithConverter<SimpleTestModel>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<SimpleTestModel>>(SimpleTestModelJson, options);

        // Assert - All properties are null initially
        Assert.NotNull(result);
        Assert.NotNull(result.Elements);
        Assert.Null(result.Elements.Title);
        Assert.Null(result.Elements.Price);
        Assert.Null(result.Elements.Count);
        Assert.Null(result.Elements.IsActive);
    }

    [Fact]
    public async Task Deserialize_ConcurrentCalls_IsThreadSafe()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();
        var tasks = new List<Task<ContentItem<Article>?>>();
        const int concurrentCalls = 100;

        // Act - perform many concurrent deserializations
        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks.Add(Task.Run(() =>
                JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all deserializations should succeed and produce valid system attributes
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.NotNull(result.System);
            Assert.Equal("test_article", result.System.Codename);
            Assert.NotNull(result.Elements);
        });
    }

    [Fact]
    public void Deserialize_PreservesRawItemJsonForPostProcessing()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();

        // Act
        var result = JsonSerializer.Deserialize<ContentItem<Article>>(SimpleArticleJson, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RawItemJson);
        Assert.Equal(JsonValueKind.Object, result.RawItemJson.Value.ValueKind);

        // RawItemJson should contain the full item structure with elements
        Assert.True(result.RawItemJson.Value.TryGetProperty("elements", out var rawElements));
        Assert.True(rawElements.TryGetProperty("title", out var titleElement));
        Assert.True(titleElement.TryGetProperty("type", out var typeValue));
        Assert.Equal("text", typeValue.GetString());

        // Should also contain the value for later extraction by ContentItemMapper
        Assert.True(titleElement.TryGetProperty("value", out var valueElement));
        Assert.Equal("Test Article Title", valueElement.GetString());
    }

    [Fact]
    public void Deserialize_MissingSystemProperty_ThrowsJsonException()
    {
        // Arrange
        var options = CreateOptionsWithConverter<Article>();
        var invalidJson = """
            {
                "elements": {
                    "title": { "type": "text", "value": "Test" }
                }
            }
            """;

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ContentItem<Article>>(invalidJson, options));
        Assert.Contains("system", exception.Message);
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
