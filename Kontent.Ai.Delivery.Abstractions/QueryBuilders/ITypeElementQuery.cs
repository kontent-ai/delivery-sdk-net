using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a content type element.
/// </summary>
public interface ITypeElementQuery
{
    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITypeElementQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery element response.</returns>
    Task<IDeliveryElementResponse> ExecuteAsync();
}
