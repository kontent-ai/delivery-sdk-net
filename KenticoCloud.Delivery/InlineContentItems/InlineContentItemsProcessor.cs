using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;

namespace KenticoCloud.Delivery.InlineContentItems
{
    /// <summary>
    /// Processor responsible for parsing HTML input and resolving inline content items referenced in them using registered resolvers
    /// </summary>
    public class InlineContentItemsProcessor : IInlineContentItemsProcessor
    {
        private readonly Dictionary<Type, Func<object, string>> _typeResolver;
        private readonly IInlineContentItemsResolver<UnretrievedContentItem> _unretrievedInlineContentItemsResolver;
        private readonly HtmlParser _htmlParser;
        private readonly HtmlParser _strictHtmlParser;

        /// <summary>
        /// Resolver used in case no other resolver was registered for type of inline content item
        /// </summary>
        public IInlineContentItemsResolver<object> DefaultResolver { get; set; }

        /// <summary>
        /// Inline content item processor, going through HTML and replacing content items marked as object elements with output of resolvers.
        /// </summary>
        /// <param name="defaultResolver">Resolver used in case no content type specific resolver was registered.</param>
        /// <param name="unretrievedInlineContentItemsResolver">Resolver whose output is used in case that value of inline content item was not retrieved from Delivery API.</param>
        public InlineContentItemsProcessor(IInlineContentItemsResolver<object> defaultResolver, IInlineContentItemsResolver<UnretrievedContentItem> unretrievedInlineContentItemsResolver)
        {
            DefaultResolver = defaultResolver;
            _typeResolver = new Dictionary<Type, Func<object, string>>();
            _unretrievedInlineContentItemsResolver = unretrievedInlineContentItemsResolver;
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
                object inlineContentItem;
                var contentItemCodename = inlineContentItemElement.GetAttribute("data-codename");
                if (inlineContentItemMap.TryGetValue(contentItemCodename, out inlineContentItem))
                {
                    string fragmentText;
                    Type inlineContentItemType;
                    var unretrieved = inlineContentItem as UnretrievedContentItem;
                    if (unretrieved != null)
                    {
                        inlineContentItemType = typeof(UnretrievedContentItem);
                        var data = new ResolvedContentItemData<UnretrievedContentItem> { Item = unretrieved };
                        fragmentText = _unretrievedInlineContentItemsResolver.Resolve(data);
                    }
                    else
                    {
                        inlineContentItemType = inlineContentItem.GetType();
                        Func<object, string> inlineContentItemResolver;
                        if (_typeResolver.TryGetValue(inlineContentItemType, out inlineContentItemResolver))
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
        {
            return htmlInput.Body.GetElementsByTagName("object").Where(o => o.GetAttribute("type") == "application/kenticocloud" && o.GetAttribute("data-type") == "item").ToList();
        }

        /// <summary>
        /// Function used for registering content type specific resolvers used during processing.
        /// </summary>
        /// <param name="resolver">Method which is used for specific content type as resolver.</param>
        /// <typeparam name="T">Content type which is resolver resolving.</typeparam>
        public void RegisterTypeResolver<T>(IInlineContentItemsResolver<T> resolver)
        {
            _typeResolver.Add(typeof(T), x => resolver.Resolve(new ResolvedContentItemData<T> { Item = (T)x }));
        }
    }
}