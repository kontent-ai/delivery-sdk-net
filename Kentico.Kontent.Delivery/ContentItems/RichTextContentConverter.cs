using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AngleSharp.Parser.Html;
using Kentico.Kontent.Delivery.Abstractions.ContentItems;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;
using Kentico.Kontent.Delivery.Abstractions.ContentTypes.Element;
using Kentico.Kontent.Delivery.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.ContentItems.RichText;
using Kentico.Kontent.Delivery.ContentItems.RichText.Blocks;
using Kentico.Kontent.Delivery.ContentTypes.Element;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Delivery.ContentItems
{
    internal class RichTextContentConverter : IPropertyValueConverter
    {
        public object GetPropertyValue(PropertyInfo property, IContentElement contentElement, ResolvingContext context)
        {
            if (!typeof(IRichTextContent).IsAssignableFrom(property.PropertyType))
            {
                throw new InvalidOperationException($"Type of property {property.Name} must implement {nameof(IRichTextContent)} in order to receive rich text content.");
            }
            if (!(contentElement is ContentElement element))
            {
                return null;
            }

            var links = ((JObject)element.Source).Property("links")?.Value;
            var images = ((JObject)element.Source).Property("images").Value;
            var value = ((JObject)element.Source).Property("value")?.Value?.ToObject<string>();

            // Handle rich_text link resolution
            if (links != null && ((JObject)element.Source) != null && context.ContentLinkUrlResolver != null)
            {
                value = new ContentLinkResolver(context.ContentLinkUrlResolver).ResolveContentLinks(value, links);
            }

            var blocks = new List<IRichTextBlock>();

            var htmlInput = new HtmlParser().Parse(value);
            foreach (var block in htmlInput.Body.Children)
            {
                if (block.TagName?.Equals("object", StringComparison.OrdinalIgnoreCase) == true && block.GetAttribute("type") == "application/kenticocloud" && block.GetAttribute("data-type") == "item")
                {
                    var codename = block.GetAttribute("data-codename");
                    blocks.Add(new InlineContentItem { ContentItem = context.GetLinkedItem(codename) });
                }
                else if (block.TagName?.Equals("figure", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var img = block.Children.FirstOrDefault(child => child.TagName?.Equals("img", StringComparison.OrdinalIgnoreCase) == true);
                    if (img != null)
                    {
                        var assetId = img.GetAttribute("data-asset-id");
                        var asset = images[assetId];
                        blocks.Add(new InlineImage
                        {
                            Url = asset.Value<string>("url"),
                            Description = asset.Value<string>("description"),
                            Height = asset.Value<int>("height"),
                            Width = asset.Value<int>("width")
                        });
                    }
                }
                else
                {
                    blocks.Add(new HtmlContent { Html = block.OuterHtml });
                }
            }

            return new RichTextContent
            {
                Blocks = blocks
            };
        }
    }
}
