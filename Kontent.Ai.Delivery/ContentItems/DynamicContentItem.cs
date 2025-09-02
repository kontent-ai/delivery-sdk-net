using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

internal sealed record DynamicContentItem : IContentItem<IReadOnlyDictionary<string, JsonElement>>
{
    [JsonPropertyName("system")]
    public required IContentItemSystemAttributes System { get; init; }

    [JsonPropertyName("elements")]
    public required IReadOnlyDictionary<string, JsonElement> Elements { get; init; }
}