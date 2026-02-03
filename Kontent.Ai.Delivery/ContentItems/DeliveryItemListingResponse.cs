using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IDeliveryItemListingResponse{TModel}" />
internal sealed record DeliveryItemListingResponse<TModel> : IDeliveryItemListingResponse<TModel>
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem<TModel>> Items { get; init; }

    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    /// <summary>
    /// Raw modular content used for resolving linked items/inline content.
    /// </summary>
    [JsonPropertyName("modular_content")]
    public required IReadOnlyDictionary<string, JsonElement> ModularContent { get; init; }

    /// <summary>
    /// Delegate to fetch the next page. Injected by the query builder.
    /// </summary>
    [JsonIgnore]
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>>>? NextPageFetcher { get; init; }

    // Expose read-only view to the interface
    IReadOnlyList<IContentItem<TModel>> IDeliveryItemListingResponse<TModel>.Items => Items;
    IPagination IPageable.Pagination => Pagination;

    /// <inheritdoc />
    public bool HasNextPage => !string.IsNullOrEmpty(Pagination.NextPageUrl);

    /// <inheritdoc />
    public async Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextPage || NextPageFetcher == null)
            return null;

        return await NextPageFetcher(cancellationToken).ConfigureAwait(false);
    }
}
