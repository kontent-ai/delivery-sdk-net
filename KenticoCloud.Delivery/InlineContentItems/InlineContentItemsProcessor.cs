using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Resolves strongly typed model into string.
    /// </summary>
    /// <param name="o">Strongly typed model.</param>
    /// <returns>String representation of the model (can be HTML, for instance).</returns>
    internal delegate string ResolveInlineContent(object o);

    /// <summary>
    /// Processor responsible for parsing HTML input and resolving inline content items referenced in them using registered resolvers
    /// </summary>
    internal class InlineContentItemsProcessor : IInlineContentItemsProcessor
    {
        private readonly IDictionary<Type, Func<object, string>> _inlineContentItemsResolvers;
        private readonly IInlineContentItemsResolver<UnretrievedContentItem> _unretrievedInlineContentItemsResolver;
        private readonly HtmlParser _htmlParser;
        private readonly HtmlParser _strictHtmlParser;

        /// <summary>
        /// Resolver used in case no other resolver was registered for type of inline content item
        /// </summary>
        public IInlineContentItemsResolver<object> DefaultResolver { get; set; }

        // Used by tests only
        internal IEnumerable<Type> ContentItemTypesWithResolver => _inlineContentItemsResolvers.Keys;

        /// <summary>
        /// Inline content item processor, going through HTML and replacing content items marked as object elements with output of resolvers.
        /// </summary>
        /// <param name="defaultResolver">Resolver used in case no content type specific resolver was registered.</param>
        /// <param name="unretrievedInlineContentItemsResolver">Resolver whose output is used in case that value of inline content item was not retrieved from Delivery API.</param>
        /// <param name="inlineContentItemsResolvers">Collection of inline content item resolvers.</param>
        public InlineContentItemsProcessor(IInlineContentItemsResolver<object> defaultResolver, IInlineContentItemsResolver<UnretrievedContentItem> unretrievedInlineContentItemsResolver, IEnumerable<ITypelessInlineContentItemsResolver> inlineContentItemsResolvers)
        {
            DefaultResolver = defaultResolver;
            _unretrievedInlineContentItemsResolver = unretrievedInlineContentItemsResolver;
            _inlineContentItemsResolvers = inlineContentItemsResolvers
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
            var document = _htmlParser.Parse(value);
            var inlineContentItemElements = GetInlineContentItemElements(document);

            foreach (var inlineContentItemElement in inlineContentItemElements)
            {
                var contentItemCodename = inlineContentItemElement.GetAttribute("data-codename");
                if (inlineContentItemMap.TryGetValue(contentItemCodename, out object inlineContentItem))
                {
                    string fragmentText;
                    Type inlineContentItemType;
                    if (inlineContentItem is UnretrievedContentItem unretrieved)
                    {
                        inlineContentItemType = typeof(UnretrievedContentItem);
                        var data = new ResolvedContentItemData<UnretrievedContentItem> { Item = unretrieved };
                        fragmentText = _unretrievedInlineContentItemsResolver.Resolve(data);
                    }
                    else
                    {
                        inlineContentItemType = inlineContentItem.GetType();
                        if (_inlineContentItemsResolvers.TryGetValue(inlineContentItemType, out var inlineContentItemResolver))
                        {
                            fragmentText = inlineContentItemResolver(inlineContentItem);
                        }
                        else
                        {
                            var data = new ResolvedContentItemData<object> { Item = inlineContentItem };
                            fragmentText = DefaultResolver.Resolve(data);
                        }
                    }

                    try
                    {
                        var fragmentNodes = _strictHtmlParser.ParseFragment(fragmentText, inlineContentItemElement.ParentElement);
                        inlineContentItemElement.Replace(fragmentNodes.ToArray());
                    }
                    catch (HtmlParseException exception)
                    {
                        var errorNode = document.CreateTextNode($"[Inline content item resolver provided an invalid HTML 5 fragment ({exception.Position.Line}:{exception.Position.Column}). Please check the output for a content item {contentItemCodename} of type {inlineContentItemType}.]");
                        inlineContentItemElement.Replace(errorNode);
                    }
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
            var htmlInput = new HtmlParser().Parse(value);
            List<IElement> inlineContentItems = GetInlineContentItemElements(htmlInput);
            foreach (var contentItem in inlineContentItems)
            {
                contentItem.Remove();
            }
            return htmlInput.Body.InnerHtml;
        }

        private static List<IElement> GetInlineContentItemElements(AngleSharp.Dom.Html.IHtmlDocument htmlInput) 
            => htmlInput
                .Body
                .GetElementsByTagName("object")
                .Where(o => o.GetAttribute("type") == "application/kenticocloud" && o.GetAttribute("data-type") == "item")
                .ToList();
    }
}