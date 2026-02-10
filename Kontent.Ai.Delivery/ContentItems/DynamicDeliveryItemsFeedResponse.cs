using System.Text.Json;

namespace Kontent.Ai.Delivery.ContentItems;

/// <summary>
/// Response for dynamic items feed queries that supports runtime type resolution.
/// Each item in the response may be a different concrete type.
/// </summary>
internal sealed record DynamicDeliveryItemsFeedResponse : IDeliveryItemsFeedResponse
{
    public required IReadOnlyList<IContentItem> Items { get; init; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, JsonElement>? ModularContent { get; init; }

    /// <summary>
    /// Delegate that fetches the next page when invoked. Injected by the query builder.
    /// </summary>
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse>>>? NextPageFetcher { get; init; }

    /// <summary>
    /// The continuation token for the next page.
    /// </summary>
    internal string? ContinuationToken { get; init; }

    /// <inheritdoc/>
    public bool HasNextPage => !string.IsNullOrEmpty(ContinuationToken);

    /// <inheritdoc/>
    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default) => !HasNextPage || NextPageFetcher is null ? null : await NextPageFetcher(cancellationToken).ConfigureAwait(false);
}
