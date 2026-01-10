using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IDeliveryItemsFeedResponse{T}" />
internal sealed record DeliveryItemsFeedResponse<TModel> : IDeliveryItemsFeedResponse<TModel>
{
    /// <inheritdoc/>
    [JsonPropertyName("items")]
    public required IReadOnlyList<ContentItem<TModel>> Items { get; init; }

    /// <summary>
    /// Raw modular content used for resolving linked items/inline content.
    /// </summary>
    [JsonPropertyName("modular_content")]
    public required Dictionary<string, JsonElement> ModularContent { get; init; }

    /// <summary>
    /// Delegate that fetches the next page when invoked. Injected by the query builder.
    /// </summary>
    [JsonIgnore]
    internal Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>>>? NextPageFetcher { get; init; }

    /// <summary>
    /// The continuation token for the next page.
    /// </summary>
    [JsonIgnore]
    internal string? ContinuationToken { get; init; }

    IReadOnlyList<IContentItem<TModel>> IDeliveryItemsFeedResponse<TModel>.Items => Items;

    /// <inheritdoc/>
    public bool HasNextPage => !string.IsNullOrEmpty(ContinuationToken);

    /// <inheritdoc/>
    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasNextPage || NextPageFetcher == null)
            return null;

        return await NextPageFetcher(cancellationToken).ConfigureAwait(false);
    }
}