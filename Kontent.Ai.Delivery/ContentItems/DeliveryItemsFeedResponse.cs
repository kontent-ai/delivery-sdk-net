using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
internal sealed record DeliveryItemsFeedResponse<TModel> : IDeliveryItemsFeedResponse<TModel>
{
    /// <inheritdoc/>
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem<TModel>> Items { get; init; } = [];

    IReadOnlyList<IContentItem<TModel>> IDeliveryItemsFeedResponse<TModel>.Items => Items;
}