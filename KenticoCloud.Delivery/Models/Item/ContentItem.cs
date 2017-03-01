using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represent a content item.
    /// </summary>
    public sealed class ContentItem
    {
        private readonly JToken _source;
        private JObject elements => (JObject)_source["elements"];
        private readonly JObject _modularContent;
        private ContentItemSystemAttributes _system;
        private dynamic _elements;

        /// <summary>
        /// Gets the system attributes of the content item.
        /// </summary>
        public ContentItemSystemAttributes System
        {
            get { return _system ?? (_system = new ContentItemSystemAttributes(_source["system"])); }
        }

        /// <summary>
        /// Gets the dynamic view of the JSON response where elements and their properties can be retrieved by name, for example <c>item.Elements.description.value</c>;
        /// </summary>
        public dynamic Elements
        {
            get { return _elements?? (_elements = JObject.Parse(_source["elements"].ToString())); }
        }

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
            _source = source;
            _modularContent = (JObject)modularContentSource;
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

            return contentItemCodenames.Where(codename => _modularContent.Property(codename) != null).Select(codename => new ContentItem(_modularContent[codename], _modularContent));
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
        /// Casts <see cref="ContentItem"/> to a strongly-typed model.
        /// </summary>
        /// <typeparam name="T">POCO type</typeparam>
        /// <returns>POCO model</returns>
        public T CastTo<T>()
        {
            return Parse<T>(_source, _modularContent);
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


        internal static T Parse<T>(JToken item, JToken modularContent)
        {
            T instance = (T)Activator.CreateInstance(typeof(T));

            foreach (var property in instance.GetType().GetProperties())
            {
                if (property.SetMethod != null)
                {
                    if (property.PropertyType == typeof(IEnumerable<ContentItem>))
                    {
                        var contentItemCodenames = ((JObject)item["elements"])
                            .Properties()
                            ?.FirstOrDefault(p => p.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                            ?.FirstOrDefault()["value"].ToObject<IEnumerable<string>>();

                        if (contentItemCodenames != null && contentItemCodenames.Any())
                        {
                            var modularContentNode = (JObject)modularContent;
                            var contentItems = new List<ContentItem>();
                            foreach (string codename in contentItemCodenames)
                            {
                                var modularContentItemNode = modularContentNode.Properties()
                                    .First(p => p.Name == codename).First;

                                if (modularContentItemNode != null)
                                {
                                    contentItems.Add(new ContentItem(modularContentItemNode, modularContentNode));
                                }
                            }

                            property.SetValue(instance, contentItems);
                        }
                    }
                    else if (property.PropertyType == typeof(IEnumerable<MultipleChoiceOption>)
                             || property.PropertyType == typeof(IEnumerable<Asset>)
                             || property.PropertyType == typeof(IEnumerable<TaxonomyTerm>)
                             || property.PropertyType == typeof(string)
                             || property.PropertyType.GetTypeInfo().IsValueType)
                    {
                        object value = ((JObject)item["elements"])
                            .Properties()
                            ?.FirstOrDefault(child => child.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                            ?.First["value"].ToObject(property.PropertyType);

                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                    else if (property.PropertyType == typeof(ContentItemSystemAttributes))
                    {
                        object value = ((JObject)item["system"]).ToObject(typeof(ContentItemSystemAttributes));

                        if (value != null)
                        {
                            property.SetValue(instance, value);
                        }
                    }
                }
            }

            return instance;
        }
    }
}
