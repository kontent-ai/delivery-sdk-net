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
    public class InlineContentItemsProcessor
    {
        private readonly Dictionary<Type, Func<object, string>> _typeResolver;

        private readonly IInlineContentItemsResolver<UnretrievedContentItem> _unretrievedInlineContentItemsResolver;

        /// <summary>
        /// Resolver used in case no other resolver was registered for type of inline content item
        /// </summary>
        public IInlineContentItemsResolver<object> DefaultResolver;

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
        }

        /// <summary>
        /// Processes HTML input and returns it with inline content items replaced with resolvers output.
        /// </summary>
        /// <param name="value">HTML code</param>
        /// <param name="usedContentItems">Content items referenced as inline content items</param>
        /// <returns>HTML with inline content items replaced with resolvers output</returns>
        public string Process(string value, Dictionary<string, object> usedContentItems)
        {
            object processedContentItem;
            var htmlInput = new HtmlParser().Parse(value);

            var inlineContentItems = GetContentItemsFromHtml(htmlInput);
            foreach (var contentItems in inlineContentItems)
            {
                var codename = contentItems.GetAttribute("data-codename");
                var wasResolved = usedContentItems.TryGetValue(codename, out processedContentItem);
                if (wasResolved)
                {
                    string replacement;
                    Type contentType;
                    var unretrieved = processedContentItem as UnretrievedContentItem;
                    if (unretrieved != null)
                    {
                        contentType = typeof(UnretrievedContentItem);
                        var data = new ResolvedContentItemData<UnretrievedContentItem> { Item = unretrieved };
                        replacement = _unretrievedInlineContentItemsResolver.Resolve(data);
                    }
                    else
                    {
                        contentType = processedContentItem.GetType();
                        Func<object, string> resolver;
                        if (_typeResolver.TryGetValue(contentType, out resolver))
                        {
                            replacement = resolver(processedContentItem);
                        }
                        else
                        {
                            var data = new ResolvedContentItemData<object> { Item = processedContentItem };
                            replacement = DefaultResolver.Resolve(data);
                        }                  
                    }

                    try
                    {
                        var options = new HtmlParserOptions()
                        {
                            IsStrictMode = true
                        };
                        var docs = new HtmlParser(options).ParseFragment(replacement, contentItems);
                        contentItems.Replace(docs.ToArray());
                    }
                    catch (HtmlParseException exception)
                    {
                        var textNodeWithError =
                            htmlInput.CreateTextNode($"Error while parsing resolvers output for content type {contentType}, codename {codename} at line {exception.Position.Line}, column {exception.Position.Column}.");
                        contentItems.Replace(textNodeWithError);
                    }
                }
            }

            return htmlInput.Body.InnerHtml;
        }

        /// <summary>
        /// Removes all content items from given HTML content.
        /// </summary>
        /// <param name="value">HTML content</param>
        /// <returns>HTML without inline content items</returns>
        public string RemoveAll(string value)
        {
            var htmlInput = new HtmlParser().Parse(value);
            List<IElement> inlineContentItems = GetContentItemsFromHtml(htmlInput);
            foreach (var contentItem in inlineContentItems)
            {
                contentItem.Remove();
            }
            return htmlInput.Body.InnerHtml;
        }

        private static List<IElement> GetContentItemsFromHtml(AngleSharp.Dom.Html.IHtmlDocument htmlInput)
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