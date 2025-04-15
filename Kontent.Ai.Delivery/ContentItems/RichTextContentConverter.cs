﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.ContentLinks;
using Kontent.Ai.Delivery.ContentItems.RichText;
using Kontent.Ai.Delivery.ContentItems.RichText.Blocks;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.ContentItems
{
    internal class RichTextContentConverter : IPropertyValueConverter<string>
    {
        public IHtmlParser Parser { get; }
        public IOptionsMonitor<DeliveryOptions> Options { get; }

        public RichTextContentConverter(IHtmlParser parser, IOptionsMonitor<DeliveryOptions> options)
        {
            Parser = parser;
            Options = options;
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
                        if (!string.IsNullOrEmpty(Options.CurrentValue.AssetUrlReplacement))
                        {
                            var assetToReplace = element.Images[assetId];
                            var replacedAsset = new InlineImage()
                            {
                                Url = ReplaceAssetUrlWIthCustomAssetUrl(assetToReplace.Url),
                                Description = assetToReplace.Description,
                                Height = assetToReplace.Height,
                                Width = assetToReplace.Width,
                                ImageId = assetToReplace.ImageId
                            };
                            blocks.Add(replacedAsset);
                        }
                        else
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

        /// <summary>
        /// Replace the beginning part of the asset URL with the AssetUrlReplacement value.
        /// </summary>
        /// <param name="url">Original Asset Url</param>
        /// <returns>New URL with the CDN URL replaces with AssetUrlReplacement</returns>
        private string ReplaceAssetUrlWIthCustomAssetUrl(string url)
        {
            // Replace the beginning part of the asset URL with the AssetUrlReplacement value by taking the third forward slash as the ending point for the string replacement
            var endOfUrlIndex = url.IndexOf("/", url.IndexOf("/", url.IndexOf("/", 0) + 1) + 1);
            if (endOfUrlIndex > 0)
            {
                return Options.CurrentValue.AssetUrlReplacement + url.Substring(endOfUrlIndex);
            }
            return url;
        }
    }
}
