using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IContentItem{TModel}" />
internal sealed record ContentItem<TModel> : IContentItem<TModel>
    where TModel : IElementsModel
{
    /// <inheritdoc/>
    [JsonPropertyName("system")]
    public required ContentItemSystemAttributes System { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("elements")]
    public required TModel Elements { get; init; }

    /// <inheritdoc/>
    IContentItemSystemAttributes IContentItem.System => System;
}