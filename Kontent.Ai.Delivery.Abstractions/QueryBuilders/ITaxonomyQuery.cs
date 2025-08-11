using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a single taxonomy group by codename.
/// </summary>
public interface ITaxonomyQuery
{
    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery taxonomy response.</returns>
    Task<IDeliveryTaxonomyResponse> ExecuteAsync();
}
