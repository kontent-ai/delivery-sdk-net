using System;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from the API when requesting content item by its codename.
    /// </summary>
    [Obsolete("Use DeliveryItemResponse<T> instead.")]
    public class DeliveryItemResponse
    {
        /// <summary>
        /// Content item.
        /// </summary>
        public ContentItem Item { get; set; }

        /// <summary>
        /// Modular content.
        /// </summary>
        public dynamic ModularContent { get; set; }

        /// <summary>
        /// Initializes response object.
        /// </summary>
        /// <param name="response">JSON returned from API.</param>
        public DeliveryItemResponse(JToken response)
        {
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Item = new ContentItem(response["item"], response["modular_content"]);
        }

        public DeliveryItemResponse(DeliveryItemResponse<ContentItem> d)
        {
            Item = d.Item;
            ModularContent = d.ModularContent;
        }
    }


    /// <summary>
    /// Represents a response from the API when requesting content item by its codename.
    /// </summary>
    public class DeliveryItemResponse<T> where T : ContentItem, new()
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
            Item = new T();
            Item.MapElementsFromJson(response["item"], response["modular_content"]);
        }
    }
}