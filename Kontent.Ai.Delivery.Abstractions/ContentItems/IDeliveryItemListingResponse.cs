using System.Text.Json;

namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of content items
/// with runtime type resolution support.
/// </summary>
/// <remarks>
/// This non-generic interface is used for dynamic queries where items may be resolved
/// to different concrete types at runtime based on the registered ITypeProvider.
/// </remarks>
public interface IDeliveryItemListingResponse : IPageable
{
    /// <summary>
    /// Gets a read-only list of content items. Each item may be a different concrete type
    /// based on runtime type resolution.
    /// </summary>
    IReadOnlyList<IContentItem> Items { get; }

    /// <summary>
    /// Raw modular content (linked items) from the API response.
    /// Useful for manually resolving linked items when working with dynamic content,
    /// where linked item elements contain codename strings rather than resolved items.
    /// </summary>
    IReadOnlyDictionary<string, JsonElement>? ModularContent { get; }

    /// <summary>
    /// Gets a value indicating whether there are more pages available.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of results if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The next page of results, or null if there are no more pages.</returns>
    Task<IDeliveryResult<IDeliveryItemListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of content items.
/// </summary>
/// <typeparam name="TModel">The type of content items in the response.</typeparam>
public interface IDeliveryItemListingResponse<TModel> : IPageable
{
    /// <summary>
    /// Gets a read-only list of content items.
    /// </summary>
    IReadOnlyList<IContentItem<TModel>> Items { get; }

    /// <summary>
    /// Gets a value indicating whether there are more pages available.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of results if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The next page of results, or null if there are no more pages.</returns>
    Task<IDeliveryResult<IDeliveryItemListingResponse<TModel>>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}
