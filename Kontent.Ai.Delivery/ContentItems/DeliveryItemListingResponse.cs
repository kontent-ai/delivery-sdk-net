using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IDeliveryItemListingResponse{TModel}" />
internal sealed record DeliveryItemListingResponse<TModel> : IDeliveryItemListingResponse<TModel>
    where TModel : IElementsModel
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem<TModel>> Items { get; init; } = [];

    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; } = default!;

    /// <summary>
    /// Raw modular content used for resolving linked items/inline content.
    /// </summary>
    [JsonPropertyName("modular_content")]
    internal required Dictionary<string, JsonElement> ModularContent { get; init; } = new();

    // Expose read-only view to the interface
    IReadOnlyList<IContentItem<TModel>> IDeliveryItemListingResponse<TModel>.Items => Items;
    IPagination IPageable.Pagination => Pagination;
}
