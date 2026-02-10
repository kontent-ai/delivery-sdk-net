using System.Text.Json;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Mapping;

public sealed class RichTextElementEnvelopeReaderTests
{
    [Fact]
    public void RichTextElementEnvelopeReader_ParsesImagesLinksAndModularContent()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "type": "rich_text",
              "name": "Body Copy",
              "value": "<p>Hello world</p>",
              "images": {
                "11111111-1111-1111-1111-111111111111": {
                  "description": "Hero",
                  "url": "https://example.com/image.jpg",
                  "height": 100,
                  "width": 200,
                  "image_id": "11111111-1111-1111-1111-111111111111"
                }
              },
              "links": {
                "22222222-2222-2222-2222-222222222222": {
                  "codename": "linked_item",
                  "url_slug": "linked-item",
                  "type": "article"
                }
              },
              "modular_content": ["component_a", "", "linked_item"]
            }
            """);

        var richText = RichTextElementEnvelopeReader.Read(
            doc.RootElement,
            "body_copy",
            preserveEmptyModularContentEntries: true);

        Assert.Equal("rich_text", richText.Type);
        Assert.Equal("Body Copy", richText.Name);
        Assert.Equal("body_copy", richText.Codename);
        Assert.Equal("<p>Hello world</p>", richText.Value);

        Assert.Single(richText.Images);
        Assert.Single(richText.Links);
        Assert.Equal(3, richText.ModularContent.Count);
    }

    [Fact]
    public void RichTextElementDataConverter_UsesSharedEnvelopeReader_ParityWithMapperPath()
    {
        using var doc = JsonDocument.Parse(
            """
            {
              "type": "rich_text",
              "name": "Body Copy",
              "codename": "body_copy",
              "value": "<p>Hello world</p>",
              "images": {
                "11111111-1111-1111-1111-111111111111": {
                  "description": "Hero",
                  "url": "https://example.com/image.jpg",
                  "height": 100,
                  "width": 200,
                  "image_id": "11111111-1111-1111-1111-111111111111"
                }
              },
              "links": {
                "22222222-2222-2222-2222-222222222222": {
                  "codename": "linked_item",
                  "url_slug": "linked-item",
                  "type": "article"
                }
              },
              "modular_content": ["component_a", "", "linked_item"]
            }
            """);

        var converterOptions = new JsonSerializerOptions
        {
            Converters = { new RichTextElementDataConverter() }
        };

        var converted = JsonSerializer.Deserialize<RichTextElementData>(doc.RootElement.GetRawText(), converterOptions);
        Assert.NotNull(converted);

        var shared = RichTextElementEnvelopeReader.Read(
            doc.RootElement,
            "body_copy",
            serializerOptions: new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            preserveEmptyModularContentEntries: false);

        Assert.Equal(shared.Type, converted!.Type);
        Assert.Equal(shared.Name, converted.Name);
        Assert.Equal(shared.Codename, converted.Codename);
        Assert.Equal(shared.Value, converted.Value);
        Assert.Equal(shared.Images.Count, converted.Images.Count);
        Assert.Equal(shared.Links.Count, converted.Links.Count);
        Assert.Equal(shared.ModularContent, converted.ModularContent);
    }
}
