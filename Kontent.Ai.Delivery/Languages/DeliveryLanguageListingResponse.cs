using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.Languages;

/// <inheritdoc cref="IDeliveryLanguageListingResponse" />
internal sealed record DeliveryLanguageListingResponse : IDeliveryLanguageListingResponse
{
    /// <inheritdoc/>
    [JsonPropertyName("languages")]
    public required IReadOnlyList<Language> Languages { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("pagination")]
    public required Pagination Pagination { get; init; }

    /// <summary>
    /// Delegate that fetches the next page when invoked. Injected by the query builder.
    /// </summary>
    [JsonIgnore]
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryLanguageListingResponse>>>? NextPageFetcher { get; init; }

    IReadOnlyList<ILanguage> IDeliveryLanguageListingResponse.Languages => Languages;
    IPagination IPageable.Pagination => Pagination;

    /// <inheritdoc/>
    public bool HasNextPage => !string.IsNullOrEmpty(Pagination.NextPageUrl);

    /// <inheritdoc/>
    public async Task<IDeliveryResult<IDeliveryLanguageListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
    {
        return !HasNextPage || NextPageFetcher is null ? null : await NextPageFetcher(cancellationToken).ConfigureAwait(false);
    }
}
