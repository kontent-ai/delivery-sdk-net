using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a single content type by codename.
/// </summary>
public interface ITypeQuery
{
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    ITypeQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Overrides the global option for waiting on the newest content for this specific request.
    /// </summary>
    /// <param name="enabled">Whether to wait for loading new content.</param>
    ITypeQuery WaitForLoadingNewContent(bool enabled = true);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery type response.</returns>
    Task<IDeliveryTypeResponse> ExecuteAsync();
}
