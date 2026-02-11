namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// Fluent builder for retrieving a single taxonomy group by codename.
/// </summary>
public interface ITaxonomyQuery
{
    /// <summary>
    /// Configures waiting for the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query and returns a functional result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A delivery result containing the taxonomy or errors.</returns>
    Task<IDeliveryResult<ITaxonomyGroup>> ExecuteAsync(CancellationToken cancellationToken = default);
}
