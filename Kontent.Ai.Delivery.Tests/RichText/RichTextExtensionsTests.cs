using FluentAssertions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.Tests.RichText;

public class RichTextExtensionsTests
{
    [Fact]
    public void GetEmbeddedContent_Generic_TraversesNestedBlocks()
    {
        var richText = CreateNestedRichTextWithEmbedded(new RichTextTestModel { Title = "Nested item" });

        var embedded = richText.GetEmbeddedContent<RichTextTestModel>().ToList();

        embedded.Should().ContainSingle();
        embedded[0].Elements.Title.Should().Be("Nested item");
    }

    [Fact]
    public void GetEmbeddedElements_Generic_TraversesNestedBlocks()
    {
        var richText = CreateNestedRichTextWithEmbedded(new RichTextTestModel { Title = "Nested item" });

        var elements = richText.GetEmbeddedElements<RichTextTestModel>().ToList();

        elements.Should().ContainSingle();
        elements[0].Title.Should().Be("Nested item");
    }

    private static RichTextContent CreateNestedRichTextWithEmbedded(RichTextTestModel model)
    {
        var embedded = new ContentItem<RichTextTestModel>
        {
            System = new ContentItemSystemAttributes
            {
                Id = Guid.NewGuid(),
                Name = "Embedded item",
                Codename = "embedded_item",
                Type = "article",
                LastModified = DateTime.UtcNow,
                Language = "en-US",
                Collection = "default"
            },
            Elements = model
        };

        var rootBlock = new HtmlNode(
            TagName: "p",
            Attributes: new Dictionary<string, string>(),
            Children: [embedded]);

        return new RichTextContent([rootBlock]);
    }

    private sealed class RichTextTestModel
    {
        public required string Title { get; init; }
    }
}
