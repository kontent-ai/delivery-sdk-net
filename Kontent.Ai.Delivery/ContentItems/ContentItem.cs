using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IContentItem{TModel}" />
/// <remarks>
/// ContentItem implements <see cref="IEmbeddedContent{TModel}"/> which allows it to be used
/// directly in rich text blocks without a wrapper class.
/// </remarks>
internal sealed record ContentItem<TModel> : IEmbeddedContent<TModel>, IRawContentItem
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
    IContentItemSystemAttributes IContentItem.System => System;
    object? IContentItem.Elements => Elements;
    object IRawContentItem.Elements => Elements!;
    JsonElement? IRawContentItem.RawElements => RawElements;
}