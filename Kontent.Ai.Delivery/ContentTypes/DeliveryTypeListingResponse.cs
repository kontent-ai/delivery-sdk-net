using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentTypes;

/// <inheritdoc cref="IDeliveryTypeListingResponse" />
internal sealed record DeliveryTypeListingResponse : IDeliveryTypeListingResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("types")]
    public required IReadOnlyList<ContentType> Types { get; init; }

    /// <summary>
    /// Delegate that fetches the next page when invoked. Injected by the query builder.
    /// </summary>
    [JsonIgnore]
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryTypeListingResponse>>>? NextPageFetcher { get; init; }

    IReadOnlyList<IContentType> IDeliveryTypeListingResponse.Types => Types;
    IPagination IPageable.Pagination => Pagination;

    /// <inheritdoc/>
    public bool HasNextPage => !string.IsNullOrEmpty(Pagination.NextPageUrl);

    /// <inheritdoc/>
    public async Task<IDeliveryResult<IDeliveryTypeListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default) => !HasNextPage || NextPageFetcher is null ? null : await NextPageFetcher(cancellationToken).ConfigureAwait(false);
}
