using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents a listing response from the API.
    /// </summary>
    public class DeliverListingResponse
    {
        /// <summary>
        /// How many content items were skipped.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// How many items the response contains.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// URL to the next page of items. If empty, there is no next page.
        /// </summary>
        public string NextPageUrl { get; set; }

        /// <summary>
        /// Content items.
        /// </summary>
        public List<ContentItem> Items { get; set; }

        /// <summary>
        /// Related items.
        /// </summary>
        public dynamic RelatedItems { get; set; }

        public DeliverListingResponse(JToken response)
        {
            Skip = response["skip"].ToObject<int>();
            Limit = response["limit"].ToObject<int>();
            Count = response["count"].ToObject<int>();
            NextPageUrl = response["next_page"].ToString();
            RelatedItems = JObject.Parse(response["related_items"].ToString());

            Items = ((JArray)response["items"]).Select(x => new ContentItem(x, response["related_items"])).ToList();
        }
    }
}
