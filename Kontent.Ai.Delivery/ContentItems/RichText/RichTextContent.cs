using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.RichText;

[method: JsonConstructor]
internal class RichTextContent() : List<IRichTextBlock>, IRichTextContent
{
}
