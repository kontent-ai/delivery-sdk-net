using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Serialization.Converters;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Serialization;

public class DynamicContentItemConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void Read_MissingSystemProperty_ThrowsJsonException()
    {
        var json = """{"elements": {}}""";

        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<ContentItem<IDynamicElements>>(json, Options));

        Assert.Contains("system", ex.Message);
    }

    [Fact]
    public void Read_MissingElementsProperty_ReturnsEmptyDynamicElements()
    {
        var json = """
        {
            "system": {
                "id": "00000000-0000-0000-0000-000000000001",
                "name": "Test",
                "codename": "test",
                "type": "article",
                "collection": "default",
                "workflow": "default",
                "workflow_step": "published",
                "language": "en-US",
                "last_modified": "2024-01-01T00:00:00Z",
                "sitemap_locations": []
            }
        }
        """;

        var result = JsonSerializer.Deserialize<ContentItem<IDynamicElements>>(json, Options)!;

        Assert.NotNull(result.System);
        Assert.Empty((DynamicElements)result.Elements);
    }

    [Fact]
    public void Write_ThrowsNotSupportedException()
    {
        var item = new ContentItem<IDynamicElements>
        {
            System = CreateSystemAttributes(),
            Elements = new DynamicElements(new Dictionary<string, JsonElement>())
        };

        Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Serialize(item, Options));
    }

    private static ContentItemSystemAttributes CreateSystemAttributes()
        => JsonSerializer.Deserialize<ContentItemSystemAttributes>("""
        {
            "id": "00000000-0000-0000-0000-000000000001",
            "name": "Test",
            "codename": "test",
            "type": "article",
            "collection": "default",
            "workflow": "default",
            "workflow_step": "published",
            "language": "en-US",
            "last_modified": "2024-01-01T00:00:00Z",
            "sitemap_locations": []
        }
        """, Options)!;

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ContentItemConverterFactory());
        return options;
    }
}
