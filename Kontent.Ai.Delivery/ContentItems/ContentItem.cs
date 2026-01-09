using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IContentItem{TModel}" />
internal sealed record ContentItem<TModel> : IContentItem<TModel>, IRawContentItem
{
    [JsonPropertyName("system")]
    public required ContentItemSystemAttributes System { get; init; }

    [JsonPropertyName("elements")]
    public required TModel Elements { get; init; }

    /// <summary>
    /// Raw JSON elements captured during deserialization for post-processing hydration.
    /// </summary>
    [JsonIgnore]
    internal JsonElement? RawElements { get; init; }

    // Explicit interface implementations
    IContentItemSystemAttributes IContentItem<TModel>.System => System;
    object IRawContentItem.Elements => Elements!;
    JsonElement? IRawContentItem.RawElements => RawElements;
}