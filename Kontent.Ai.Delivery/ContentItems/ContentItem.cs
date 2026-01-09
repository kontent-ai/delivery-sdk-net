using System.Text.Json.Serialization;
using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IContentItem{TModel}" />
internal sealed record ContentItem<TModel> : IContentItem<TModel>, IRawContentItem
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required ContentItemSystemAttributes System { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("elements")]
    public required TModel Elements { get; init; }

    IContentItemSystemAttributes IContentItem<TModel>.System => System;

    // Captured raw elements JsonElement for post-processing (not serialized)
    [JsonIgnore]
    internal JsonElement? RawElements { get; init; }

    object IRawContentItem.Elements => Elements!;
    JsonElement? IRawContentItem.RawElements => RawElements;
}