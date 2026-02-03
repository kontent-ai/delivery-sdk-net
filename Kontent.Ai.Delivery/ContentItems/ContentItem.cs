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
    /// Raw JSON of the full content item captured during deserialization for post-processing hydration
    /// and runtime type resolution. Contains system, elements, and any other API response properties.
    /// </summary>
    [JsonIgnore]
    internal JsonElement? RawItemJson { get; init; }

    // Explicit interface implementations
    IContentItemSystemAttributes IContentItem.System => System;
    object? IContentItem.Elements => Elements;
    JsonElement? IRawContentItem.RawItemJson => RawItemJson;
}
