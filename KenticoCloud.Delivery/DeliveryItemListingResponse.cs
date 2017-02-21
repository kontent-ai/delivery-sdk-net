using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace KenticoCloud.Delivery
{
    /// <summary>
    /// Represents item listing response from the API.
    /// </summary>
    public class DeliveryItemListingResponse
    {
        /// <summary>
        /// Contains paging information.
        /// </summary>
        public Pagination Pagination { get; set; }

        /// <summary>
        /// Content items.
        /// </summary>
        public List<ContentItem> Items { get; set; }

        /// <summary>
        /// Modular content.
        /// </summary>
        public dynamic ModularContent { get; set; }

        /// <summary>
        /// Initializes response object.
        /// </summary>
        /// <param name="response">JSON returned from API.</param>
        public DeliveryItemListingResponse(JToken response)
        {
            Pagination = new Pagination(response["pagination"]);
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Items = ((JArray)response["items"]).Select(x => new ContentItem(x, response["modular_content"])).ToList();
        }
    }
}