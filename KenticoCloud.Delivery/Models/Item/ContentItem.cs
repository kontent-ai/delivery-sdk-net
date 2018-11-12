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
        private readonly JToken _linkedItemsSource;
        private readonly IContentLinkUrlResolver _contentLinkUrlResolver;
        private readonly ICodeFirstModelProvider _codeFirstModelProvider;

        private ContentItemSystemAttributes _system;
        private JToken _elements;

        internal Lazy<ContentLinkResolver> ContentLinkResolver;

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
        /// <param name="linkedItemsSource">The JSON data of linked items to deserialize.</param>
        /// <param name="contentLinkUrlResolver">An instance of an object that can resolve links in rich text elements</param>
        /// <param name="codeFirstModelProvider">An instance of an object that can JSON responses into strongly typed CLR objects</param>
        internal ContentItem(JToken source, JToken linkedItemsSource, IContentLinkUrlResolver contentLinkUrlResolver, ICodeFirstModelProvider codeFirstModelProvider)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _linkedItemsSource = linkedItemsSource ?? throw new ArgumentNullException(nameof(linkedItemsSource));
            _codeFirstModelProvider = codeFirstModelProvider ?? throw new ArgumentNullException(nameof(codeFirstModelProvider));

            _contentLinkUrlResolver = contentLinkUrlResolver;
            ContentLinkResolver = new Lazy<ContentLinkResolver>(() =>
                _contentLinkUrlResolver != null
                    ? new ContentLinkResolver(_contentLinkUrlResolver)
                    : null
            );
        }

        /// <summary>
        /// Casts the item to a code-first model.
        /// </summary>
        /// <typeparam name="T">Type of the code-first model.</typeparam>
        public T CastTo<T>()
        {
            return _codeFirstModelProvider.GetContentItemModel<T>(_source, _linkedItemsSource);
        }

        /// <summary>
        /// Gets a string value from an element and resolves content links in Rich text element values.
        /// To resolve content links an instance of <see cref="IContentLinkUrlResolver"/> must be set to the <see cref="IDeliveryClient"/> instance.
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

            return contentLinkResolver.Value.ResolveContentLinks(value, links);
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
        /// Returns a collection of linked content items by the element name.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of content items that are assigned to the element with the specified codename.</returns>
        /// <remarks>
        /// The collection contains only content items that are included in the response from the Delivery API as linked items.
        /// For more information see the <c>Linked Items</c> topic in the Delivery API documentation.
        /// </remarks>
        public IEnumerable<ContentItem> GetLinkedItems(string elementCodename)
        {
            var element = GetElement(elementCodename);
            var contentItemCodenames = ((JArray)element["value"]).Values<string>();

            return contentItemCodenames
                .Where(codename => _linkedItemsSource[codename] != null)
                .Select(codename => new ContentItem(_linkedItemsSource[codename], _linkedItemsSource, _contentLinkUrlResolver, _codeFirstModelProvider));
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
