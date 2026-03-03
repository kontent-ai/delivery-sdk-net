using System.Text.Json;
using Kontent.Ai.Delivery.ContentTypes.Element;
using Kontent.Ai.Delivery.Serialization.Converters;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.Serialization;

public class ContentElementDictionaryConverterTests
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    [Fact]
    public void Read_InvalidRootToken_ThrowsJsonException()
    {
        var json = "[1, 2, 3]";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, Options));
    }

    [Fact]
    public void Read_ValidElements_HydratesCodenameFromKey()
    {
        var json = """
        {
            "title": { "type": "text", "name": "Title" },
            "body": { "type": "rich_text", "name": "Body" }
        }
        """;

        var result = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ContentElement>>(json, Options)!;

        Assert.Equal(2, result.Count);
        Assert.Equal("title", result["title"].Codename);
        Assert.Equal("body", result["body"].Codename);
    }

    [Fact]
    public void Write_ThrowsNotSupportedException()
    {
        var dict = new Dictionary<string, ContentElement>() as IReadOnlyDictionary<string, ContentElement>;

        Assert.Throws<NotSupportedException>(() =>
            JsonSerializer.Serialize(dict, Options));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ContentElementConverter());
        options.Converters.Add(new ContentElementDictionaryConverter());
        return options;
    }
}
