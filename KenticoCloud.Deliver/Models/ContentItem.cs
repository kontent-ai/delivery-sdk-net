using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KenticoCloud.Deliver
{
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
            System = new SystemElements(item["system"]);
            Elements = JObject.Parse(item["elements"].ToString());

            elements = (JObject)item["elements"];
            this.relatedItems = (JObject)relatedItems;
        }


        public string GetString(string element)
        {
            return GetElementValue<string>(element);
        }


        public double GetNumber(string element)
        {
            return GetElementValue<double>(element);
        }


        public DateTime GetDatetime(string element)
        {
            return GetElementValue<DateTime>(element);
        }


        public IEnumerable<ContentItem> GetModularContent(string element)
        {
            var codenames = ((JArray)elements[element]["value"]).ToObject<List<string>>();

            return codenames.Select(c => new ContentItem(relatedItems[c], relatedItems));
        }


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
