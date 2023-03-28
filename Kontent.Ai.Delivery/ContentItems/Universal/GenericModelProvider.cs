using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.ContentItems.Universal
{
    internal class GenericModelProvider : IUniversalItemModelProvider
    {
        internal readonly JsonSerializer Serializer;

        internal GenericModelProvider(JsonSerializer serializer)
        {
            Serializer = serializer;
        }

        public async Task<IUniversalContentItem> GetContentItemGenericModelAsync(object item)
        => (IUniversalContentItem)await GetContentItemModelAsync((JObject)item);

        internal async Task<IUniversalContentItem> GetContentItemModelAsync(JObject serializedItem)
        {
            var result = new UniversalContentItem() {
                System = serializedItem?["system"]?.ToObject<IContentItemSystemAttributes>(Serializer)
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
                    "rich_text" => element.ToObject<RichTextElementValue>(Serializer).WithCodename(key),
                    "asset" => element.ToObject<AssetElementValue>(Serializer).WithCodename(key),
                    "number" => element.ToObject<NumberElementValue>(Serializer).WithCodename(key),
                    // TODO do we want to use string/structured data for date time => structured is OK
                    "date_time" => element.ToObject<DateTimeElementValue>(Serializer).WithCodename(key),
                    "multiple_choice" => element.ToObject<MultipleChoiceElementValue>(Serializer).WithCodename(key),
                    "taxonomy" => element.ToObject<TaxonomyElementValue>(Serializer).WithCodename(key),
                    // TODO what Linked items + what SubPages? 
                    "modular_content" => element.ToObject<ContentElementValue<IEnumerable<string>>>(Serializer).WithCodename(key),
                    "custom" => element.ToObject<CustomElementValue>(Serializer).WithCodename(key),
                    "url_slug" => element.ToObject<UrlSlugElementValue>(Serializer).WithCodename(key),
                    "text" => element.ToObject<TextElementValue>(Serializer).WithCodename(key),
                    _ => throw new ArgumentException($"Argument type ({element["type"].ToString()}) not supported.")
                };
                result.Elements.Add(key, value);
            }

            return result;
        }
    }
}