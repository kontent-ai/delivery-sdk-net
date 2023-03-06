using Kontent.Ai.Delivery.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Kontent.Ai.Delivery.ContentItems.Elements
{
    internal class RichTextElementValue : StringElementValue, IRichTextElementValue
    {
        [JsonProperty("images")]
        public IDictionary<Guid, IInlineImage> Images { get; set; }

        [JsonProperty("links")]
        public IDictionary<Guid, IContentLink> Links { get; set; }

        [JsonProperty("modular_content")]
        public List<string> ModularContent { get; set; }
    }
}
