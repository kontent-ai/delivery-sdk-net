using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems.Elements;
using Kontent.Ai.Delivery.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kontent.Ai.Delivery.ContentItems.Universal
{
    internal class UniversalItemModelProvider : IUniversalItemModelProvider
    {
        internal readonly JsonSerializer Serializer;

        public UniversalItemModelProvider(JsonSerializer serializer)
        {
            Serializer = serializer;
        }

        public async Task<IUniversalContentItem> GetContentItemGenericModelAsync(object item)
        => (IUniversalContentItem)await GetContentItemModelAsync((JObject)item);

        internal Task<IUniversalContentItem> GetContentItemModelAsync(JObject serializedItem)
        {
            IUniversalContentItem result = new UniversalContentItem() {
                System = serializedItem?["system"]?.ToObject<IContentItemSystemAttributes>(Serializer)
            };

            foreach (var item in (JObject)serializedItem["elements"])
            {
                var key = item.Key;
                var element = item.Value;

                IContentElementValue value = (element["type"].ToString()) switch
                {
                    "rich_text" => element.ToObject<RichTextElementValue>(Serializer).WithCodename(key),
                    "asset" => element.ToObject<AssetElementValue>(Serializer).WithCodename(key),
                    "number" => element.ToObject<NumberElementValue>(Serializer).WithCodename(key),
                    "date_time" => element.ToObject<DateTimeElementValue>(Serializer).WithCodename(key),
                    "multiple_choice" => element.ToObject<MultipleChoiceElementValue>(Serializer).WithCodename(key),
                    "taxonomy" => element.ToObject<TaxonomyElementValue>(Serializer).WithCodename(key),
                    "modular_content" => element.ToObject<LinkedItemsElementValue>(Serializer).WithCodename(key),
                    "custom" => element.ToObject<CustomElementValue>(Serializer).WithCodename(key),
                    "url_slug" => element.ToObject<UrlSlugElementValue>(Serializer).WithCodename(key),
                    "text" => element.ToObject<TextElementValue>(Serializer).WithCodename(key),
                    _ => throw new ArgumentException($"Argument type ({element["type"].ToString()}) not supported.")
                };
                result.Elements.Add(key, value);
            }

            return Task.FromResult(result);
        }
    }
}