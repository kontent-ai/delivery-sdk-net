using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents a response from the API when requesting content item by its codename.
    /// </summary>
    public class DeliverItemResponse
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
        public DeliverItemResponse(JToken response)
        {
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Item = new ContentItem(response["item"], response["modular_content"]);
        }
    }
}
