using System.Collections.Generic;
using Kentico.Kontent.Delivery.Abstractions;
using Newtonsoft.Json;

namespace Kentico.Kontent.Delivery.ContentItems.RichText
{
    internal class RichTextContent : List<IRichTextBlock>, IRichTextContent
    {
        [JsonConstructor]
        public RichTextContent()
        {
        }
    }
}
