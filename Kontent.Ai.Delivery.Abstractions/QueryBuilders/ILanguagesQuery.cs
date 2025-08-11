using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for listing languages.
/// </summary>
public interface ILanguagesQuery
{
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    ILanguagesQuery OrderBy(string elementOrAttributePath, bool ascending = true);

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
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery languages listing response.</returns>
    Task<IDeliveryLanguageListingResponse> ExecuteAsync();
}
