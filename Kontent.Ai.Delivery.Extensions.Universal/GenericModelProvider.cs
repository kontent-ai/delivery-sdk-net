using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.Extensions.Universal
{
    internal class UniversalContentItemModelProvider
    {
        public static async Task<IUniversalContentItem> GetContentItemGenericModelAsync(object item, JsonSerializer serializer)
        => (IUniversalContentItem)await GetContentItemModelAsync((JObject)item, serializer);

        internal static async Task<IUniversalContentItem> GetContentItemModelAsync(JObject serializedItem, JsonSerializer serializer)
        {
            var result = new UniversalContentItem() {
                System = serializedItem?["system"]?.ToObject<IContentItemSystemAttributes>(serializer)
            };

            foreach (var item in (JObject)serializedItem["elements"])
            {
                var key = item.Key;
                var element = item.Value;

                // TODO think about value converter implementation
                // TODO what about codename property - now it is not null, bit withCodename is not really nice
                IContentElementValue value = (element["type"].ToString()) switch
                {
                    // TODO do we want to use string/structured data for rich text - probably think about support both ways
                    "rich_text" => element.ToObject<RichTextElementValue>(serializer).WithCodename(key),
                    "asset" => element.ToObject<AssetElementValue>(serializer).WithCodename(key),
                    "number" => element.ToObject<NumberElementValue>(serializer).WithCodename(key),
                    // TODO do we want to use string/structured data for date time => structured is OK
                    "date_time" => element.ToObject<DateTimeElementValue>(serializer).WithCodename(key),
                    "multiple_choice" => element.ToObject<MultipleChoiceElementValue>(serializer).WithCodename(key),
                    "taxonomy" => element.ToObject<TaxonomyElementValue>(serializer).WithCodename(key),
                    // TODO what Linked items + what SubPages? 
                    "modular_content" => element.ToObject<LinkedItemsElementValue>(serializer).WithCodename(key),
                    "custom" => element.ToObject<CustomElementValue>(serializer).WithCodename(key),
                    "url_slug" => element.ToObject<UrlSlugElementValue>(serializer).WithCodename(key),
                    "text" => element.ToObject<TextElementValue>(serializer).WithCodename(key),
                    _ => throw new ArgumentException($"Argument type ({element["type"].ToString()}) not supported.")
                };
                result.Elements.Add(key, value);
            }

            return result;
        }
    }
}