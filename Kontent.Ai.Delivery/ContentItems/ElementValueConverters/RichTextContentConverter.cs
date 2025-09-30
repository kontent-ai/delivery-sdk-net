using System.Reflection;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;

namespace Kontent.Ai.Delivery.ContentItems;

internal class RichTextContentConverter(IHtmlParser parser) : IPropertyValueConverter<string, IRichTextContent>
{
    public async Task<IRichTextContent?> GetPropertyValueAsync<TElement>(
        PropertyInfo property,
        TElement contentElement,
        ResolvingContext context) where TElement : IContentElementValue<string>
    {
        if (!typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IRichTextContent)} in order to receive rich text content.");

        if (contentElement is not IRichTextElementValue element)
            return null;

        var html = await ResolveContentLinksAsync(element, context);
        var document = await parser.ParseDocumentAsync(html);

        var blocks = await Task.WhenAll(
            document.Body.Children.Select(block => ParseBlockAsync(block, element, context))
        );

        var content = new RichTextContent();
        content.AddRange(blocks);
        return content;
    }

    private static async Task<string> ResolveContentLinksAsync(IRichTextElementValue element, ResolvingContext context) =>
        element.Links is not null && context.ContentLinkUrlResolver is not null
            ? await new ContentLinkResolver(context.ContentLinkUrlResolver).ResolveContentLinksAsync(element.Value, element.Links)
            : element.Value;

    private static async Task<IRichTextBlock> ParseBlockAsync(
        IElement block,
        IRichTextElementValue element,
        ResolvingContext context) =>
        block switch
        {
            { TagName: "OBJECT" } when IsInlineContentItem(block)
                => new InlineContentItem(await context.GetLinkedItem(block.GetAttribute("data-codename")!)),
            { TagName: "FIGURE" } when TryGetInlineImage(block, element, out var image)
                => image,
            _ => new HtmlContent { Html = block.OuterHtml }
        };

    private static bool IsInlineContentItem(IElement block) =>
        block.GetAttribute("type") == "application/kenticocloud" &&
        block.GetAttribute("data-type") == "item";

    private static bool TryGetInlineImage(
        IElement figureBlock,
        IRichTextElementValue element,
        out IInlineImage image)
    {
        var img = figureBlock.Children
            .FirstOrDefault(child => child.TagName?.Equals("img", StringComparison.OrdinalIgnoreCase) == true);

        if (img is not null &&
            Guid.TryParse(img.GetAttribute("data-asset-id"), out var assetId) &&
            element.Images.TryGetValue(assetId, out var inlineImage))
        {
            image = inlineImage;
            return true;
        }

        image = null!;
        return false;
    }
}
