using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a content item.
    /// </summary>
    public sealed class ContentItem
    {
        private readonly JToken _source;
        private readonly JToken _modularContentSource;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private readonly ICodeFirstModelProvider _codeFirstModelProvider;
        private ContentLinkResolver _contentLinkResolver;

        private ContentItemSystemAttributes _system;
        private JToken _elements;

        internal ContentLinkResolver ContentLinkResolver
        {
            get
            {
                if (_contentLinkResolver == null && _contentLinkUrlResolver != null)
                {
                    _contentLinkResolver = new ContentLinkResolver(_contentLinkUrlResolver);
                }
                return _contentLinkResolver;
            }
        }

        /// <summary>
        /// Gets the system attributes of the content item.
        /// </summary>
        public ContentItemSystemAttributes System
        {
            get { return _system ?? (_system = _source["system"].ToObject<ContentItemSystemAttributes>()); }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where elements and their properties can be retrieved by name, for example <c>item.Elements.description.value</c>;
        /// </summary>
        public dynamic Elements
        {
            get { return _elements ?? (_elements = _source["elements"].DeepClone()); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItem"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data of the content item to deserialize.</param>
        /// <param name="modularContentSource">The JSON data of modular content to deserialize.</param>
        /// <param name="client">The client that retrieved the content item.</param>
        internal ContentItem(JToken source, JToken modularContentSource, IContentLinkUrlResolver contentLinkUrlResolver, ICodeFirstModelProvider codeFirstModelProvider)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (modularContentSource == null)
            {
                throw new ArgumentNullException(nameof(modularContentSource));
            }

            if (contentLinkUrlResolver == null)
            {
                throw new ArgumentNullException(nameof(contentLinkUrlResolver));
            }

            if (codeFirstModelProvider == null)
            {
                throw new ArgumentNullException(nameof(codeFirstModelProvider));
            }

            _source = source;
            _modularContentSource = modularContentSource;
            _contentLinkUrlResolver = contentLinkUrlResolver;
            _codeFirstModelProvider = codeFirstModelProvider;
        }

        /// <summary>
        /// Casts the item to a code-first model.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model.</typeparam>
        public T CastTo<T>()
        {
            return _codeFirstModelProvider.GetContentItemModel<T>(_source, _modularContentSource);
        }

        /// <summary>
        /// Gets a string value from an element and resolves content links in Rich text element values.
        /// To resolve content links the <see cref="IDeliveryClient.ContentLinkUrlResolver"/> property must be set.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>The <see cref="string"/> value of the element with the specified codename, if available; otherwise, <c>null</c>.</returns>
        public string GetString(string elementCodename)
        {
            var element = GetElement(elementCodename);
            var value = element.Value<string>("value");
            var elementType = element.Value<string>("type");
            var links = element["links"];
            var contentLinkResolver = ContentLinkResolver;

            if (!StringComparer.Ordinal.Equals(elementType, "rich_text") || links == null || contentLinkResolver == null || string.IsNullOrEmpty(value) || !value.Contains("data-item-id"))
            {
                return value;
            }

            return contentLinkResolver.ResolveContentLinks(value, links);
        }

        /// <summary>
        /// Returns the <see cref="decimal"/> value of the specified Number element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>The <see cref="decimal"/> value of the element with the specified codename, if available; otherwise, <c>null</c>.</returns>
        public decimal? GetNumber(string elementCodename)
        {
            return GetElementValue<decimal?>(elementCodename);
        }

        /// <summary>
        /// Returns the <see cref="DateTime"/> value of the specified Date and time element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>The <see cref="DateTime"/> value of the element with the specified codename, if available; otherwise, <c>null</c>.</returns>
        public DateTime? GetDateTime(string elementCodename)
        {
            return GetElementValue<DateTime?>(elementCodename);
        }

        /// <summary>
        /// Returns a collection of content items that are assigned to the specified Modular content element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of content items that are assigned to the element with the specified codename.</returns>
        /// <remarks>
        /// The collection contains only content items that are included in the response from the Delivery API as modular content.
        /// For more information see the <c>Modular content</c> topic in the Delivery API documentation.
        /// </remarks>
        public IEnumerable<ContentItem> GetModularContent(string elementCodename)
        {
            var element = GetElement(elementCodename);
            var contentItemCodenames = ((JArray)element["value"]).Values<string>();

            return contentItemCodenames
                .Where(codename => _modularContentSource[codename] != null)
                .Select(codename => new ContentItem(_modularContentSource[codename], _modularContentSource, _contentLinkUrlResolver, _codeFirstModelProvider));
        }

        /// <summary>
        /// Returns a collection of assets that were uploaded to the specified Asset element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of assets that were uploaded to the element with the specified codename.</returns>
        public IEnumerable<Asset> GetAssets(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => source.ToObject<Asset>());
        }

        /// <summary>
        /// Returns a collection of options that were selected in the specified Multiple choice element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A list of selected options of the element with the specified codename.</returns>
        public IEnumerable<MultipleChoiceOption> GetOptions(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => source.ToObject<MultipleChoiceOption>());
        }

        /// <summary>
        /// Returns a collection of taxonomy terms that are assigned to the specified Taxonomy element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of taxonomy terms that are assigned to the element with the specified codename.</returns>
        public IEnumerable<TaxonomyTerm> GetTaxonomyTerms(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => source.ToObject<TaxonomyTerm>());
        }

        /// <summary>
        /// Returns a value of the specified element.
        /// </summary>
        /// <typeparam name="T">The type of the element value.</typeparam>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A value of the element with the specified codename.</returns>
        private T GetElementValue<T>(string elementCodename)
        {
            if (elementCodename == null)
            {
                throw new ArgumentNullException(nameof(elementCodename));
            }

            if (_source["elements"][elementCodename] == null)
            {
                throw new ArgumentException($"Element with the specified codename does not exist: {elementCodename}", nameof(elementCodename));
            }

            return _source["elements"][elementCodename]["value"].ToObject<T>();
        }

        /// <summary>
        /// Returns the specified element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>The element with the specified codename.</returns>
        private JToken GetElement(string elementCodename)
        {
            if (elementCodename == null)
            {
                throw new ArgumentNullException(nameof(elementCodename));
            }

            if (_source["elements"][elementCodename] == null)
            {
                throw new ArgumentException($"Element with the specified codename does not exist: {elementCodename}", nameof(elementCodename));
            }

            return _source["elements"][elementCodename];
        }
    }
}
