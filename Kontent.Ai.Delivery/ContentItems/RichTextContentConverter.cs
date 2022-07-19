using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.ContentItems.RichText;
using Kentico.Kontent.Delivery.ContentItems.RichText.Blocks;

namespace Kentico.Kontent.Delivery.ContentItems
{
    internal class RichTextContentConverter : IPropertyValueConverter<string>
    {
        public IHtmlParser Parser { get; }

        public RichTextContentConverter(IHtmlParser parser)
        {
            Parser = parser;
        }

        public async Task<object> GetPropertyValueAsync<TElement>(PropertyInfo property, TElement contentElement, ResolvingContext context) where TElement : IContentElementValue<string>
        {
            if (!typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IRichTextContent)} in order to receive rich text content.");
            }

            if (!(contentElement is IRichTextElementValue element))
            {
                return null;
            }

            var links = element.Links;
            var value = element.Value;

            // Handle rich_text link resolution
            if (links != null && context.ContentLinkUrlResolver != null)
            {
                value = await new ContentLinkResolver(context.ContentLinkUrlResolver).ResolveContentLinksAsync(value, links);
            }

            var blocks = new RichTextContent();
            var htmlInput = await Parser.ParseDocumentAsync(value);
            foreach (var block in htmlInput.Body.Children)
            {
                if (block.TagName?.Equals("object", StringComparison.OrdinalIgnoreCase) == true && block.GetAttribute("type") == "application/kenticocloud" && block.GetAttribute("data-type") == "item")
                {
                    var codename = block.GetAttribute("data-codename");
                    blocks.Add(new InlineContentItem(await context.GetLinkedItem(codename)));
                }
                else if (block.TagName?.Equals("figure", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var img = block.Children.FirstOrDefault(child => child.TagName?.Equals("img", StringComparison.OrdinalIgnoreCase) == true);
                    if (img != null)
                    {
                        var assetId = Guid.Parse(img.GetAttribute("data-asset-id"));
                        blocks.Add(element.Images[assetId]);
                    }
                }
                else
                {
                    blocks.Add(new HtmlContent { Html = block.OuterHtml });
                }
            }

            return blocks;
        }
    }
}
