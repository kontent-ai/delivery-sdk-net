using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for listing taxonomy groups.
/// </summary>
public interface ITaxonomiesQuery
{
    /// <summary>
    /// Sets the number of taxonomy groups to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    ITaxonomiesQuery Skip(int skip);

    /// <summary>
    /// Sets the maximum number of taxonomy groups to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    ITaxonomiesQuery Limit(int limit);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery taxonomy listing response.</returns>
    Task<IDeliveryTaxonomyListingResponse> ExecuteAsync();
}
