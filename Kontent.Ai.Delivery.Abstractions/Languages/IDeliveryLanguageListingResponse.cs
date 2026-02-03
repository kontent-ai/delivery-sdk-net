namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Represents a response from Kontent.ai Delivery API that contains a list of languages.
/// </summary>
public interface IDeliveryLanguageListingResponse : IPageable
{
    /// <summary>
    /// Gets a read-only list of languages.
    /// </summary>
    IReadOnlyList<ILanguage> Languages { get; }

    /// <summary>
    /// Gets a value indicating whether there are more languages to fetch.
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// Fetches the next page of languages if available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next page of languages, or null if no more pages exist.</returns>
    Task<IDeliveryResult<IDeliveryLanguageListingResponse>?> FetchNextPageAsync(CancellationToken cancellationToken = default);
}
