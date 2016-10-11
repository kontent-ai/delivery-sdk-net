using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represents a reeponse from the API when requesting content item by its codename.
    /// </summary>
    public class DeliverResponse
    {
        /// <summary>
        /// Content item.
        /// </summary>
        public ContentItem Item { get; set; }

        /// <summary>
        /// Modular content.
        /// </summary>
        public dynamic ModularContent { get; set; }

        public DeliverResponse(JToken response)
        {
            ModularContent = JObject.Parse(response["modular_content"].ToString());
            Item = new ContentItem(response["item"], response["modular_content"]);
        }
    }
}
