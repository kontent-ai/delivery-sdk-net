using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.Tests.RichText;

public class RichTextContentTests
{
    [Fact]
    public async Task Empty_IsParagraphWithLineBreak()
    {
        var sut = RichTextContent.Empty;

        Assert.Single(sut);
        Assert.Equal("<p><br></p>", await sut.ToHtmlAsync());
    }

    [Fact]
    public void Empty_IsSharedSingleton()
    {
        Assert.Same(RichTextContent.Empty, RichTextContent.Empty);
    }

    [Fact]
    public void Count_ReturnsNumberOfBlocks()
    {
        var block = new TextNode("");
        var sut = new RichTextContent();
        sut.AddRange([block]);

        Assert.Single(sut);
    }

    [Fact]
    public void Indexer_ReturnsBlockAtPosition()
    {
        var block = new TextNode("");
        var sut = new RichTextContent();
        sut.AddRange([block]);

        Assert.Same(block, sut[0]);
    }
}
