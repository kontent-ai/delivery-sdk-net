using Kontent.Ai.Delivery.Abstractions;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Tests.Models.ContentTypes;

public class SimpleRichText : IElementsModel
{
    [JsonPropertyName("rich_text")]
    public required IRichTextContent RichText { get; init; }

    [JsonPropertyName("rich_text")]
    public required string RichTextString { get; init; }
}