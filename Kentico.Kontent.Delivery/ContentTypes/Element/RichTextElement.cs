using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Kentico.Kontent.Delivery.ContentTypes.Element
{
    internal class RichTextElement : ContentElement, IRichTextElement
    {
        [JsonProperty("images")]
        public IDictionary<Guid, IInlineImage> Images { get; set; }

        [JsonProperty("links")]
        public IDictionary<Guid, IContentLink> Links { get; set; }

        [JsonProperty("modular_content")]
        public List<string> ModularContent { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public RichTextElement() : base()
        {
        }
    }
}
