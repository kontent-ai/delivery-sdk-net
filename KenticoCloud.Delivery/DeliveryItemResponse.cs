using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

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

        private T Parse(JToken response)
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ElementValueConverter<string>());
            settings.Converters.Add(new ElementValueConverter<decimal?>());
            settings.Converters.Add(new ElementValueConverter<DateTime?>());
            settings.Converters.Add(new ElementValueConverter<IEnumerable>());

            return response.SelectToken("$.item.elements").ToObject<T>(JsonSerializer.Create(settings));
        }
    }

    public class ElementValueConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(T));
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JObject.Load(reader).SelectToken("value").ToObject<T>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}