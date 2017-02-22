using System;
using Newtonsoft.Json.Linq;

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
    public class DeliveryItemResponse<T> where T : IContentItemBased, new()
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
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Item = new ContentItem(response["item"], response["modular_content"]).CastTo<T>();
        }
    }
}