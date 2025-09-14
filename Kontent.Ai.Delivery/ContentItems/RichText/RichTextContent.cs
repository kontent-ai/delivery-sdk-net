using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems.RichText;

/// <inheritdoc cref="IRichTextContent" />
[method: JsonConstructor]
public class RichTextContent() : List<IRichTextBlock>, IRichTextContent
{
}
