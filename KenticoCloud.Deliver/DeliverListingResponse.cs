using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class DeliverListingResponse
    {
        public int Skip { get; set; }
        public int Limit { get; set; }
        public int Count { get; set; }
        public string NextPageUrl { get; set; }
        public List<ContentItem> Items { get; set; }
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
