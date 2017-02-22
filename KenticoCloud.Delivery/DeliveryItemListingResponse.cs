using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents a response from Kentico Cloud Delivery API that contains a list of content items.
    /// </summary>
    public sealed class DeliveryItemListingResponse
    {
        /// <summary>
        /// Gets paging information.
        /// </summary>
        public Pagination Pagination { get; }

        /// <summary>
        /// Gets a list of content items.
        /// </summary>
        public IReadOnlyList<ContentItem> Items { get; }

        /// <summary>
        /// Gets the dynamic view of the JSON response where modular content items and their properties can be retrieved by name, for example <c>ModularContent.about_us.elements.description.value</c>.
        /// </summary>
        public dynamic ModularContent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryItemListingResponse"/> class with information from a response.
        /// </summary>
        /// <param name="response">A response from Kentico Cloud Delivery API that contains a list of content items.</param>
        internal DeliveryItemListingResponse(JToken response)
        {
            Pagination = new Pagination(response["pagination"]);
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Items = ((JArray)response["items"]).Select(source => new ContentItem(source, response["modular_content"])).ToList().AsReadOnly();
        }
    }
}
