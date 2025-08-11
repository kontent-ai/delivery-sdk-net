using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for listing content types.
/// </summary>
public interface ITypesQuery
{
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    ITypesQuery WithElements(params string[] elementCodenames);

    /// <summary>
    /// Sets the number of types to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    ITypesQuery Skip(int skip);

    /// <summary>
    /// Sets the maximum number of types to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    ITypesQuery Limit(int limit);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery types listing response.</returns>
    Task<IDeliveryTypeListingResponse> ExecuteAsync();
}
