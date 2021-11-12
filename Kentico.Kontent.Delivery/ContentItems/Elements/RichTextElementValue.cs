using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.ContentLinks;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.Elements;
using Kentico.Kontent.Delivery.Abstractions.ContentItems.RichText.Blocks;

namespace Kentico.Kontent.Delivery.ContentItems.Elements
{
    internal class RichTextElementValue : ContentElementValue<string>, IRichTextElementValue
    {
        [JsonProperty("images")]
        public IDictionary<Guid, IInlineImage> Images { get; set; }

        [JsonProperty("links")]
        public IDictionary<Guid, IContentLink> Links { get; set; }

        [JsonProperty("modular_content")]
        public List<string> ModularContent { get; set; }
    }
}
