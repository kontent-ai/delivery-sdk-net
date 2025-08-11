using System.Threading.Tasks;

namespace Kontent.Ai.Delivery.Abstractions.QueryBuilders;

/// <summary>
/// Fluent builder for retrieving a single content item by codename.
/// </summary>
public interface ISingleItemQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    ISingleItemQuery<T> WithLanguage(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    ISingleItemQuery<T> WithElements(params string[] elementCodenames);
    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    ISingleItemQuery<T> WithoutElements(params string[] elementCodenames);
    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    ISingleItemQuery<T> Depth(int depth);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery item response.</returns>
    Task<IDeliveryItemResponse<T>> ExecuteAsync();
}
