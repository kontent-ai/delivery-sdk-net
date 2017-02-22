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
    public class ContentItem
    {
        private JObject elements;
        private JObject modularContent;

        /// <summary>
        /// <see cref="Delivery.ItemSystem"/>
        /// </summary>
        public ItemSystem System { get; set; }

        /// <summary>
        /// Elements in its raw form.
        /// </summary>
        public dynamic Elements { get; set; }

        /// <summary>
        /// Initializes content item from response JSONs.
        /// </summary>
        /// <param name="item">JSON with item data.</param>
        /// <param name="modularContent">JSON with modular content.</param>
        public ContentItem(JToken item, JToken modularContent)
        {
            if (item == null || !item.HasValues)
            {
                return;
            }

            System = new ItemSystem(item["system"]);
            Elements = JObject.Parse(item["elements"].ToString());

            elements = (JObject)item["elements"];
            this.modularContent = (JObject)modularContent;
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
        /// <param name="element">Element name.</param>
        /// <returns>Returns null if element has no value.</returns>
        public string GetString(string element)
        {
            return GetElementValue<string>(element);
        }

        /// <summary>
        /// Gets a number value from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        /// <returns>Returns null if element has no value.</returns>
        public decimal? GetNumber(string element)
        {
            return GetElementValue<decimal?>(element);
        }

        /// <summary>
        /// Gets a <see cref="DateTime"/> value from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        /// <returns>Returns null if element has no value.</returns>
        public DateTime? GetDateTime(string element)
        {
            return GetElementValue<DateTime?>(element);
        }

        /// <summary>
        /// Gets modular content from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        /// <remarks>If the modular content items are contained
        /// in the response, they will be present in this list. If not, there will be "empty"
        /// content items.</remarks>
        public IEnumerable<ContentItem> GetModularContent(string element)
        {
            if (elements.Property(element) == null)
            {
                throw new ArgumentException("Given element doesn't exist.");
            }

            var codenames = ((JArray)elements[element]["value"]).ToObject<List<string>>();

            return codenames.Select(c => new ContentItem(modularContent[c], modularContent));
        }

        /// <summary>
        /// Get <see cref="Asset"/>s from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        public List<Asset> GetAssets(string element)
        {
            if (elements.Property(element) == null)
            {
                throw new ArgumentException("Given element doesn't exist.");
            }

            return ((JArray)elements[element]["value"]).Select(x => new Asset(x)).ToList();
        }

        /// <summary>
        /// Returns the selected options of the specified multiple choice element.
        /// </summary>
        /// <param name="element">The codename of the multiple choice element.</param>
        /// <returns>A list of selected options of the specified multiple choice element.</returns>
        public List<Option> GetOptions(string element)
        {
            if (elements.Property(element) == null)
            {
                throw new ArgumentException("Given element doesn't exist.");
            }

            return ((JArray)elements[element]["value"]).Select(x => new Option(x)).ToList();
        }

        /// <summary>
        /// Returns the taxonomy terms assigned to the specified taxonomy element.
        /// </summary>
        /// <param name="element">The codename of the taxonomy element.</param>
        /// <returns>A list of taxonomy terms assigned to the specified taxonomy element.</returns>
        public List<TaxonomyTerm> GetTaxonomyTerms(string element)
        {
            if (elements.Property(element) == null)
            {
                throw new ArgumentException("Given element doesn't exist.");
            }

            return ((JArray)elements[element]["value"]).Select(x => new TaxonomyTerm(x)).ToList();
        }

        private T GetElementValue<T>(string element)
        {
            if (elements.Property(element) == null)
            {
                throw new ArgumentException("Given element doesn't exist.");
            }

            return elements[element]["value"].ToObject<T>();
        }
    }
}