namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for listing languages.
/// </summary>
public interface ILanguagesQuery
{
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="orderingMode">Ordering mode (ascending/descending).</param>
    ILanguagesQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending);

    /// <summary>
    /// Sets the number of languages to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    ILanguagesQuery Skip(int skip);

    /// <summary>
    /// Sets the maximum number of languages to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    ILanguagesQuery Limit(int limit);

    /// <summary>
    /// Configures waiting for the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ILanguagesQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// Use <see cref="IDeliveryLanguageListingResponse.FetchNextPageAsync"/> to retrieve subsequent pages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the languages with pagination support.</returns>
    Task<IDeliveryResult<IDeliveryLanguageListingResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
