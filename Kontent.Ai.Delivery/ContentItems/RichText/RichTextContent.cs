using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kontent.Ai.Delivery.ContentItems.RichText
{
    internal class RichTextContent : List<IRichTextBlock>, IRichTextContent
    {
        [JsonConstructor]
        public RichTextContent()
        {
        }
    }
}
