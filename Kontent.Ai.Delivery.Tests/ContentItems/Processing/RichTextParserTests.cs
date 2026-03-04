using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Processing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kontent.Ai.Delivery.Tests.ContentItems.Processing;

public class RichTextParserTests
{
    [Fact]
    public async Task ConvertAsync_NonRichTextElement_ReturnsNull()
    {
        var parser = RichTextParser.CreateDefault();
        var element = new PlainTextElement("plain text");

        var result = await parser.ConvertAsync(
            element,
            _ => Task.FromResult<object?>(null),
            dependencyContext: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConvertAsync_EmbeddedObjectWithoutCodename_ThrowsInvalidOperationException()
    {
        var parser = RichTextParser.CreateDefault();
        var element = new TestRichTextElement("<object type=\"application/kenticocloud\"></object>");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            parser.ConvertAsync(
                element,
                _ => Task.FromResult<object?>(null),
                dependencyContext: null));

        Assert.Contains("data-codename", exception.Message);
    }

    [Fact]
    public async Task ConvertAsync_FigureWithoutImageElement_IsIgnored()
    {
        var parser = RichTextParser.CreateDefault();
        var element = new TestRichTextElement("<figure><span>No image tag</span></figure>");

        var result = await parser.ConvertAsync(
            element,
            _ => Task.FromResult<object?>(null),
            dependencyContext: null);

        Assert.NotNull(result);
        Assert.Empty(result.GetInlineImages());
    }

    [Fact]
    public async Task ConvertAsync_FigureWithInvalidAssetId_IsIgnored()
    {
        var parser = RichTextParser.CreateDefault();
        var element = new TestRichTextElement("<figure><img data-asset-id=\"not-a-guid\" /></figure>");

        var result = await parser.ConvertAsync(
            element,
            _ => Task.FromResult<object?>(null),
            dependencyContext: null);

        Assert.NotNull(result);
        Assert.Empty(result.GetInlineImages());
    }

    [Fact]
    public async Task ConvertAsync_FigureWithUnknownAssetId_IsIgnored()
    {
        var parser = new RichTextParser(new HtmlParser(), NullContentDependencyExtractor.Instance, NullLogger.Instance);
        var missingAssetId = Guid.NewGuid();
        var element = new TestRichTextElement($"<figure><img data-asset-id=\"{missingAssetId}\" /></figure>");

        var result = await parser.ConvertAsync(
            element,
            _ => Task.FromResult<object?>(null),
            dependencyContext: null);

        Assert.NotNull(result);
        Assert.Empty(result.GetInlineImages());
    }

    private sealed class PlainTextElement(string value) : IContentElementValue<string>
    {
        public string Value { get; } = value;
        public string Codename => "plain_text";
        public string Name => "Plain text";
        public string Type => "text";
    }

    private sealed class TestRichTextElement(string value) : IRichTextElementValue
    {
        public string Value { get; } = value;
        public string Codename => "body";
        public string Name => "Body";
        public string Type => "rich_text";
        public IReadOnlyDictionary<Guid, IInlineImage> Images { get; } = new Dictionary<Guid, IInlineImage>();
        public IReadOnlyDictionary<Guid, IContentLink> Links { get; } = new Dictionary<Guid, IContentLink>();
        public IReadOnlyList<string> ModularContent { get; } = [];
    }
}
