using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.Tests.RichText;

public class RichTextContentTests
{
    [Fact]
    public void Count_ReturnsNumberOfBlocks()
    {
        var block = new ContentItemLink();
        var sut = new RichTextContent();
        sut.AddRange([block]);

        Assert.Single(sut);
    }

    [Fact]
    public void Indexer_ReturnsBlockAtPosition()
    {
        var block = new ContentItemLink();
        var sut = new RichTextContent();
        sut.AddRange([block]);

        Assert.Same(block, sut[0]);
    }
}
