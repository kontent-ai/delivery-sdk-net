using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
    public class DeliverResponse
    {
        public ContentItem Item { get; set; }
        public dynamic RelatedItems { get; set; }

        public DeliverResponse(JToken response)
        {
            RelatedItems = JObject.Parse(response["related_items"].ToString());
            Item = new ContentItem(response["item"], response["related_items"]);
        }
    }
}
