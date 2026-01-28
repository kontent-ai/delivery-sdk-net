using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Response for dynamic item listing queries that supports runtime type resolution.
/// Each item in the response may be a different concrete type.
/// </summary>
internal sealed record DynamicDeliveryItemListingResponse : IDeliveryItemListingResponse
{
    public required IReadOnlyList<IContentItem> Items { get; init; }

    public required Pagination Pagination { get; init; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, JsonElement>? ModularContent { get; init; }

    /// <summary>
    /// Delegate to fetch the next page. Injected by the query builder.
    /// </summary>
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse>>>? NextPageFetcher { get; init; }

    // Expose pagination to the interface
    IPagination IPageable.Pagination => Pagination;

    /// <inheritdoc />
    public bool HasNextPage => !string.IsNullOrEmpty(Pagination.NextPageUrl);

    /// <inheritdoc />
    public async Task<IDeliveryResult<IDeliveryItemListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextPage || NextPageFetcher == null)
            return null;

        return await NextPageFetcher(cancellationToken).ConfigureAwait(false);
    }
}
