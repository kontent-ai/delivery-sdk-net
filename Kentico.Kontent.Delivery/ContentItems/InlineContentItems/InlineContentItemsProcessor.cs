using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Builders.DeliveryClient;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Kontent.Delivery.ContentItems.InlineContentItems
{
    /// <summary>
    /// Processor responsible for parsing HTML input and resolving inline content items referenced in them using registered resolvers
    /// </summary>
    internal class InlineContentItemsProcessor : IInlineContentItemsProcessor
    {
        private readonly IDictionary<Type, Func<object, string>> _inlineContentItemsResolvers;
        private readonly HtmlParser _htmlParser;
        private readonly HtmlParser _strictHtmlParser;
        
        internal IReadOnlyDictionary<Type, Func<object, string>> ContentItemResolvers => new ReadOnlyDictionary<Type, Func<object, string>>(_inlineContentItemsResolvers);

        /// <summary>
        /// Inline content item processor, going through HTML and replacing content items marked as object elements with output of resolvers.
        /// </summary>
        /// <remarks>
        /// The collection of resolvers may contain a custom <see cref="object"/> resolver used when no content type specific resolver was registered,
        /// a custom resolver for <see cref="UnretrievedContentItem"/>s used when an item was not retrieved from Delivery API, 
        /// and a resolver for <see cref="UnknownContentItem"/> that a <see cref="IModelProvider"/> was unable to strongly type.
        /// If these resolvers are not specified using <see cref="DeliveryClientBuilder"/> or the <see cref="IServiceCollection"/> registration
        /// (<see cref="Extensions.ServiceCollectionExtensions.AddDeliveryInlineContentItemsResolver{TContentItem,TInlineContentItemsResolver}"/>),
        /// the default implementations resulting in warning messages will be used.
        /// </remarks>
        /// <param name="inlineContentItemsResolvers">Collection of inline content item resolvers.</param>
        public InlineContentItemsProcessor(IEnumerable<ITypelessInlineContentItemsResolver> inlineContentItemsResolvers)
        {
            _inlineContentItemsResolvers = inlineContentItemsResolvers
                .GroupBy(descriptor => descriptor.ContentItemType)
                .Select(descriptorGroup => descriptorGroup.Last())
                .ToDictionary<ITypelessInlineContentItemsResolver, Type, Func<object, string>>(
                    descriptor => descriptor.ContentItemType,
                    descriptor => descriptor.ResolveItem);
            _htmlParser = new HtmlParser();
            _strictHtmlParser = new HtmlParser(new HtmlParserOptions
            {
                IsStrictMode = true
            });
        }

        /// <summary>
        /// Processes HTML input and returns it with inline content items replaced with resolvers output.
        /// </summary>
        /// <param name="value">HTML code</param>
        /// <param name="inlineContentItemMap">Content items referenced as inline content items</param>
        /// <returns>HTML with inline content items replaced with resolvers output</returns>
        public string Process(string value, Dictionary<string, object> inlineContentItemMap)
        {
            var document = _htmlParser.ParseDocument(value);
            var inlineContentItemElements = GetInlineContentItemElements(document);

            foreach (var inlineContentItemElement in inlineContentItemElements)
            {
                var contentItemCodename = inlineContentItemElement.GetAttribute("data-codename");
                if (inlineContentItemMap.TryGetValue(contentItemCodename, out object inlineContentItem))
                {
                    var fragmentText = GetFragmentText(inlineContentItem);

                    ReplaceElementWithFragmentNodes(document, inlineContentItemElement, contentItemCodename, inlineContentItem, fragmentText);
                }
            }

            return document.Body.InnerHtml;
        }

        /// <summary>
        /// Removes all content items from given HTML content.
        /// </summary>
        /// <param name="value">HTML content</param>
        /// <returns>HTML without inline content items</returns>
        public string RemoveAll(string value)
        {
            var htmlInput = new HtmlParser().ParseDocument(value);
            List<IElement> inlineContentItems = GetInlineContentItemElements(htmlInput);
            foreach (var contentItem in inlineContentItems)
            {
                contentItem.Remove();
            }
            return htmlInput.Body.InnerHtml;
        }

        private static List<IElement> GetInlineContentItemElements(IHtmlDocument htmlInput) 
            => htmlInput
                .Body
                .GetElementsByTagName("object")
                .Where(o => o.GetAttribute("type") == "application/kenticocloud" && o.GetAttribute("data-type") == "item")
                .ToList();

        private string GetFragmentText(object inlineContentItem)
        {
            var inlineContentItemType = GetInlineContentItemType(inlineContentItem);
            if (_inlineContentItemsResolvers.TryGetValue(inlineContentItemType, out var inlineContentItemResolver))
            {
                return inlineContentItemResolver(inlineContentItem);
            }

            if (_inlineContentItemsResolvers.TryGetValue(typeof(object), out var defaultContentItemResolver))
            {
                return defaultContentItemResolver(inlineContentItem);
            }

            return "Default inline content item resolver for non specific content type was not registered.";
        }

        private static Type GetInlineContentItemType(object inlineContentItem)
            => inlineContentItem?.GetType() ?? typeof(UnknownContentItem);

        private void ReplaceElementWithFragmentNodes(IHtmlDocument document, IElement inlineContentItemElement, string contentItemCodename, object inlineContentItem, string fragmentText)
        {
            try
            {
                var fragmentNodes = _strictHtmlParser.ParseFragment(fragmentText, inlineContentItemElement.ParentElement);
                inlineContentItemElement.Replace(fragmentNodes.ToArray());
            }
            catch (HtmlParseException exception)
            {
                var errorNode = document.CreateTextNode($"[Inline content item resolver provided an invalid HTML 5 fragment ({exception.Position.Line}:{exception.Position.Column}). Please check the output for a content item {contentItemCodename} of type {GetInlineContentItemType(inlineContentItem)}.]");
                inlineContentItemElement.Replace(errorNode);
            }
        }
    }
}
