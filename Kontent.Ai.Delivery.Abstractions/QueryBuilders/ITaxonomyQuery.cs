using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a single taxonomy group by codename.
/// </summary>
public interface ITaxonomyQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name=\"enabled\">Whether to wait for loading new content.</param>
    ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery taxonomy response.</returns>
    Task<IDeliveryTaxonomyResponse> ExecuteAsync();
}
