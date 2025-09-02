using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

internal sealed record ContentItem<TElements> : IContentItem<TElements>
{
    [JsonPropertyName("system")]
    public required IContentItemSystemAttributes System { get; init; }

    [JsonPropertyName("elements")]
    public required TElements Elements { get; init; }
}