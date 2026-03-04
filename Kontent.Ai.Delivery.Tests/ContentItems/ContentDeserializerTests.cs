using System.Text.Json;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Configuration;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Tests.ContentItems;

public class ContentDeserializerTests
{
    private static readonly JsonSerializerOptions Options = RefitSettingsProvider.CreateDefaultJsonSerializerOptions();

    private const string ValidJson = """
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
        },
        "elements": {}
    }
    """;

    [Fact]
    public void DeserializeContentItem_String_NullJson_ThrowsArgumentException()
    {
        var sut = new ContentDeserializer(Options);

        Assert.Throws<ArgumentException>(() => sut.DeserializeContentItem(null!, typeof(IDynamicElements)));
    }

    [Fact]
    public void DeserializeContentItem_String_EmptyJson_ThrowsArgumentException()
    {
        var sut = new ContentDeserializer(Options);

        Assert.Throws<ArgumentException>(() => sut.DeserializeContentItem("", typeof(IDynamicElements)));
    }

    [Fact]
    public void DeserializeContentItem_String_NullModelType_ThrowsArgumentNullException()
    {
        var sut = new ContentDeserializer(Options);

        Assert.Throws<ArgumentNullException>(() => sut.DeserializeContentItem(ValidJson, null!));
    }

    [Fact]
    public void DeserializeContentItem_String_ValidJson_ReturnsContentItem()
    {
        var sut = new ContentDeserializer(Options);

        var result = sut.DeserializeContentItem(ValidJson, typeof(IDynamicElements));

        Assert.NotNull(result);
        var contentItem = Assert.IsType<ContentItem<IDynamicElements>>(result);
        Assert.Equal("test", contentItem.System.Codename);
    }

    [Fact]
    public void DeserializeContentItem_JsonElement_NullModelType_ThrowsArgumentNullException()
    {
        var sut = new ContentDeserializer(Options);
        var element = JsonSerializer.Deserialize<JsonElement>(ValidJson);

        Assert.Throws<ArgumentNullException>(() => sut.DeserializeContentItem(element, null!));
    }

    [Fact]
    public void DeserializeContentItem_JsonElement_ValidJson_ReturnsContentItem()
    {
        var sut = new ContentDeserializer(Options);
        var element = JsonSerializer.Deserialize<JsonElement>(ValidJson);

        var result = sut.DeserializeContentItem(element, typeof(IDynamicElements));

        Assert.NotNull(result);
        var contentItem = Assert.IsType<ContentItem<IDynamicElements>>(result);
        Assert.Equal("test", contentItem.System.Codename);
    }
}
