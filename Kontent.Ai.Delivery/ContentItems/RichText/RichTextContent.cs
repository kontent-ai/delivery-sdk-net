using System.Text.Json.Serialization;

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
