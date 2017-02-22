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
}
