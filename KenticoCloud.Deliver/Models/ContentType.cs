using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represent a content type.
    /// </summary>
    public class ContentType
    {
        private JObject elements;

        /// <summary>
        /// <see cref="Deliver.TypeSystem"/> 
        /// </summary>
        public TypeSystem System { get; set; }
        
        /// <summary>
        /// Elements in its raw form.
        /// </summary>
        public dynamic Elements { get; set; }

        /// <summary>
        /// Initializes content type from response JSONs.
        /// </summary>
        /// <param name="item">JSON with type data.</param>
        public ContentType(JToken item)
        {
            if (item == null || !item.HasValues)
            {
                return;
            }

            System = new TypeSystem(item["system"]);
            Elements = JObject.Parse(item["elements"].ToString());

            elements = (JObject)item["elements"];
        }
    }
}
