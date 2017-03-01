using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a content item.
    /// </summary>
    public sealed class DeliveryItemResponse
    {
        /// <summary>
        /// Gets the content item from the response.
        /// </summary>
        public ContentItem Item { get; }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a content item.</param>
        internal DeliveryItemResponse(JToken response)
        {
            Item = new ContentItem(response["item"], response["modular_content"]);
            ModularContent = JObject.Parse(response["modular_content"].ToString());
        }
    }


    /// <summary>
    /// Represents a response from the API when requesting content item by its codename.
    /// </summary>
    public class DeliveryItemResponse<T>
    {
        /// <summary>
        /// Content item.
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// Modular content.
        /// </summary>
        public dynamic ModularContent { get; set; }

        /// <summary>
        /// Initializes response object with a JSON response.
        /// </summary>
        /// <param name="response">JSON returned from API.</param>
        public DeliveryItemResponse(JToken response)
        {
            Item = Parse(response);
            ModularContent = JObject.Parse(response["modular_content"].ToString());
        }

        //private T Parse(JToken response)
        //{
        //    JObject fields = new JObject();
        //    foreach (var property in ((JObject)response.SelectToken("$.item.elements")).Properties())
        //    {
        //        // Remove underscore characters to support loading into PascalCase property names in CSharp code
        //        string propertyName = property.Name.Replace("_", "");
        //        fields.Add(propertyName, property.First["value"]);
        //    }

        //    return fields.ToObject<T>();
        //}

        private T Parse(JToken rootObject)
        {
            T instance = (T)Activator.CreateInstance(typeof(T));

            foreach (var property in instance.GetType().GetProperties())
            {
                if (property.SetMethod == null)
                {
                    continue;
                }

                if (property.PropertyType == typeof(IEnumerable<ContentItem>))
                {
                    var contentItemCodenames = ((JObject)rootObject["item"]["elements"])
                        .Properties()
                        .First(p => p.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                        .First["value"].ToObject<IEnumerable<string>>();

                    if (contentItemCodenames == null || contentItemCodenames.Count() <= 0)
                    {
                        continue;
                    }

                    var modularContentNode = ((JObject)rootObject["modular_content"]);

                    var contentItems = new List<ContentItem>();
                    foreach (string codename in contentItemCodenames)
                    {
                        var modularContentItemNode = modularContentNode.Properties()
                            .First(p => p.Name == codename).First;

                        if (modularContentItemNode == null)
                        {
                            continue;
                        }

                        contentItems.Add(new ContentItem(modularContentItemNode, modularContentNode));
                    }

                    property.SetValue(instance, contentItems);
                }

                if (property.PropertyType == typeof(IEnumerable<MultipleChoiceOption>) 
                    || property.PropertyType == typeof(IEnumerable<Asset>)
                    || property.PropertyType == typeof(IEnumerable<TaxonomyTerm>)
                    || property.PropertyType == typeof(DateTime?)
                    || property.PropertyType == typeof(decimal?)
                    || property.PropertyType == typeof(string))
                {
                    object value = ((JObject)rootObject["item"]["elements"])
                        .Properties()
                        .First(child => child.Name.Replace("_", "").ToLower() == property.Name.ToLower())
                        .First["value"].ToObject(property.PropertyType);

                    if (value == null)
                    {
                        continue;
                    }

                    property.SetValue(instance, value);
                }
            }

            return instance;
        }
    }
}