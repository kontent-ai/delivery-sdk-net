using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using AngleSharp.Parser.Xml;
using AngleSharp.Services.Default;

namespace KenticoCloud.Delivery.ContentItemsInRichText
{
    /// <summary>
    /// Processor responsible for parsing richtext elements and resolving content items referenced in them using provided resolvers.
    /// </summary>
    public class ContentItemsInRichTextProcessor
    {
        private readonly Dictionary<Type, Func<object, string>> _typeResolver;

        private readonly IContentItemsInRichTextResolver<UnretrievedContentItem> _unretrievedContentItemsInRichTextResolver;

        public IContentItemsInRichTextResolver<object> DefaultResolver;

        /// <summary>
        /// Rich text output processor, going through HTML and replacing content items marked as object elements with output of resolvers.
        /// </summary>
        /// <param name="defaultResolver">Resolver used in case we haven't registered content type specific resolver.</param>
        /// <param name="unretrievedContentItemsInRichTextResolver">Resolver whose output is used in case we haven't retrieved value of content item in richtext, 
        /// this can happen if depth of client request is too low.</param>
        public ContentItemsInRichTextProcessor(IContentItemsInRichTextResolver<object> defaultResolver, IContentItemsInRichTextResolver<UnretrievedContentItem> unretrievedContentItemsInRichTextResolver)
        {
            DefaultResolver = defaultResolver;
            _typeResolver = new Dictionary<Type, Func<object, string>>();
            _unretrievedContentItemsInRichTextResolver = unretrievedContentItemsInRichTextResolver;
        }

        /// <summary>
        /// Processes richtext output, which is expected to be in HTML format and returns it with resolved content items.
        /// </summary>
        /// <param name="value">Richtext output.</param>
        /// <param name="usedContentItems">Content items used in richtext, used as input for resolution function.</param>
        /// <returns></returns>
        public string Process(string value, Dictionary<string, object> usedContentItems)
        {
            object contentItem;
            var htmlRichText = new HtmlParser().Parse(value);         

            var modularContentItemsInRichText = htmlRichText.Body.GetElementsByTagName("object").Where(o => o.GetAttribute("type") == "application/kenticocloud" &&  o.GetAttribute("data-type") == "item").ToList();
            foreach (var modularContentItem in modularContentItemsInRichText)
            {
                var codename = modularContentItem.GetAttribute("data-codename");
                var wasResolved = usedContentItems.TryGetValue(codename, out contentItem);
                if (wasResolved)
                {
                    string replacement;
                    Type contentType;
                    var unresolved = contentItem as UnretrievedContentItem;
                    if (unresolved != null)
                    {
                        contentType = typeof(UnretrievedContentItem);
                        var wrapper = new ResolvedContentItemData<UnretrievedContentItem> { Item = unresolved };
                        replacement = _unretrievedContentItemsInRichTextResolver.Resolve(wrapper);
                    }
                    else
                    {
                        contentType = contentItem.GetType();
                        Func<object, string> resolver;
                        if (_typeResolver.TryGetValue(contentType, out resolver))
                        {
                            replacement = resolver(contentItem);
                        }
                        else
                        {
                            var wrapper = new ResolvedContentItemData<object> { Item = contentItem };
                            replacement = DefaultResolver.Resolve(wrapper);
                        }
                       
                    }

                    try
                    {
                        var options = new HtmlParserOptions()
                        {
                            IsStrictMode = true
                        };
                        var docs = new HtmlParser(options).ParseFragment(replacement, modularContentItem);
                        modularContentItem.Replace(docs.ToArray());
                    }
                    catch (HtmlParseException exception)
                    {
                        var textNodeWithError =
                            htmlRichText.CreateTextNode($"Error while parsing resolvers output for content type {contentType}, codename {codename} at line {exception.Position.Line}, column {exception.Position.Column}");
                        modularContentItem.Replace(textNodeWithError);
                    }

                    
   
                }
            }

            return htmlRichText.Body.InnerHtml;
        }

        /// <summary>
        /// Function used for registering content type specific resolvers used during processing.
        /// </summary>
        /// <param name="resolver">Method which is used for specific content type as resolver.</param>
        /// <typeparam name="T">Content type which is resolver resolving.</typeparam>
        public void RegisterTypeResolver<T>(IContentItemsInRichTextResolver<T> resolver)
        {
            _typeResolver.Add(typeof(T), x => resolver.Resolve(new ResolvedContentItemData<T> { Item = (T)x }));
        }
    }
}