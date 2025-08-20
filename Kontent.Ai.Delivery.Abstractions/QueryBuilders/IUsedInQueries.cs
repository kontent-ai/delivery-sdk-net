using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving content items that use the specified item.
/// </summary>
public interface IItemUsedInQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery items feed response for used in items.</returns>
    Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync();
}

/// <summary>
/// Fluent builder for retrieving content items that use the specified asset.
/// </summary>
public interface IAssetUsedInQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery items feed response for used in items.</returns>
    Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync();
}
