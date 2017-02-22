using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Defines a method which is called during casting from <see cref="ContentItem"/> to more specific type.
    /// </summary>
    public interface IContentItemBased
    {
        void LoadFromContentItem(ContentItem contentItem);
    }

    /// <summary>
    /// Represent a content item.
    /// </summary>
    public sealed class ContentItem
    {
        private readonly JObject elements;
        private readonly JObject modularContent;

        /// <summary>
        /// Gets the system attributes of the content item.
        /// </summary>
        public ContentItemSystemAttributes System { get; }

        /// <summary>
        /// Gets the dynamic view of the JSON response where elements and their properties can be retrieved by name, for example <c>item.Elements.description.value</c>;
        /// </summary>
        public dynamic Elements { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentItem"/> class with the specified JSON data.
        /// </summary>
        /// <param name="source">The JSON data of the content item to deserialize.</param>
        /// <param name="modularContentSource">The JSON data of modular content to deserialize.</param>
        internal ContentItem(JToken source, JToken modularContentSource)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (modularContentSource == null)
            {
                throw new ArgumentNullException(nameof(modularContentSource));
            }

            System = new ContentItemSystemAttributes(source["system"]);
            Elements = JObject.Parse(source["elements"].ToString());

            elements = (JObject)source["elements"];
            modularContent = (JObject)modularContentSource;
        }

        /// <summary>
        /// Casts current instance to a strongly typed model implementing <see cref="IContentItemBased"/> interface. 
        /// </summary>
        public T CastTo<T>() where T : IContentItemBased, new()
        {
            var stronglyTypedModel = new T();
            stronglyTypedModel.LoadFromContentItem(this);
            return stronglyTypedModel;
        }

        /// <summary>
        /// Gets a string value from an element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>The <see cref="string"/> value of the element with the specified codename, if available; otherwise, <c>null</c>.</returns>
        public string GetString(string elementCodename)
        {
            return GetElementValue<string>(elementCodename);
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
        /// Returns the <see cref="DateTime"/> value of the specified Date & time element.
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

            return contentItemCodenames.Where(codename => modularContent.Property(codename) != null).Select(codename => new ContentItem(modularContent[codename], modularContent));
        }

        /// <summary>
        /// Returns a collection of assets that were uploaded to the specified Asset element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of assets that were uploaded to the element with the specified codename.</returns>
        public IEnumerable<Asset> GetAssets(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => new Asset(source));
        }

        /// <summary>
        /// Returns a collection of options that were selected in the specified Multiple choice element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A list of selected options of the element with the specified codename.</returns>
        public IEnumerable<MultipleChoiceOption> GetOptions(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => new MultipleChoiceOption(source));
        }

        /// <summary>
        /// Returns a collection of taxonomy terms that are assigned to the specified Taxonomy element.
        /// </summary>
        /// <param name="elementCodename">The codename of the element.</param>
        /// <returns>A collection of taxonomy terms that are assigned to the element with the specified codename.</returns>
        public IEnumerable<TaxonomyTerm> GetTaxonomyTerms(string elementCodename)
        {
            var element = GetElement(elementCodename);

            return ((JArray)element["value"]).Select(source => new TaxonomyTerm(source));
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

            if (elements.Property(elementCodename) == null)
            {
                throw new ArgumentException($"Element with the specified codename does not exist: {elementCodename}", nameof(elementCodename));
            }

            return elements[elementCodename]["value"].ToObject<T>();
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

            if (elements.Property(elementCodename) == null)
            {
                throw new ArgumentException($"Element with the specified codename does not exist: {elementCodename}", nameof(elementCodename));
            }

            return elements[elementCodename];
        }
    }
}
