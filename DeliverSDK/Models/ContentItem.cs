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

        public SystemElements System { get; set; }


        public ContentItem()
        {
        }


        public ContentItem(JToken item, JToken relatedItems)
        {
            System = new SystemElements(item["system"]);
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


        public Dictionary<string, ContentItem> GetModularContent(string element)
        {
            var codenames = ((JArray)elements[element]["value"]).ToObject<List<string>>();

            return codenames.ToDictionary(c => c, c => new ContentItem(relatedItems[c], relatedItems));
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
