using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace KenticoCloud.Deliver
{
    /// <summary>
    /// Represent a content item.
    /// </summary>
    public class ContentItem
    {
        private JObject elements;
        private JObject relatedItems;

        /// <summary>
        /// System elements.
        /// </summary>
        public SystemElements System { get; set; }
        
        /// <summary>
        /// Elements in its raw form.
        /// </summary>
        public dynamic Elements { get; set; }
        
        public ContentItem(JToken item, JToken relatedItems)
        {
            if (item == null || !item.HasValues)
            {
                return;
            }

            System = new SystemElements(item["system"]);
            Elements = JObject.Parse(item["elements"].ToString());

            elements = (JObject)item["elements"];
            this.relatedItems = (JObject)relatedItems;
        }

        /// <summary>
        /// Gets a string value from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        public string GetString(string element)
        {
            return GetElementValue<string>(element);
        }

        /// <summary>
        /// Gets a number value from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        public double GetNumber(string element)
        {
            return GetElementValue<double>(element);
        }

        /// <summary>
        /// Gets a datetime value from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        public DateTime GetDatetime(string element)
        {
            return GetElementValue<DateTime>(element);
        }

        /// <summary>
        /// Gets modular content from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        /// <remarks>If the modular content items are contained
        /// in the response, they will be present in this list. If not, there will be "empty"
        /// content items.</remarks>
        public IEnumerable<ContentItem> GetModularContent(string element)
        {
            var codenames = ((JArray)elements[element]["value"]).ToObject<List<string>>();

            return codenames.Select(c => new ContentItem(relatedItems[c], relatedItems));
        }

        /// <summary>
        /// Get assets from an element.
        /// </summary>
        /// <param name="element">Element name.</param>
        public List<Asset> GetAssets(string element)
        {
            return ((JArray)elements[element]["value"]).Select(x => new Asset(x)).ToList();
        }

        private T GetElementValue<T>(string element)
        {
            JToken token;
            return elements.TryGetValue(element, StringComparison.OrdinalIgnoreCase, out token) ? token["value"].ToObject<T>() : default(T);
        }
    }
}
