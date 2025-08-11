using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving content items that use the specified item.
/// </summary>
public interface IItemUsedInQuery
{
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
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery items feed response for used in items.</returns>
    Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync();
}
