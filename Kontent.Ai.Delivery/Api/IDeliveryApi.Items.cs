using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Api.QueryParams.Items;
using Refit;

namespace Kontent.Ai.Delivery.Api;

/// <summary>
/// Refit interface for Kontent.ai Delivery API.
/// </summary>
public partial interface IDeliveryApi
{
    /// <summary>
    /// Gets a single content item by its codename.
    /// </summary>
    /// <param name="codename">The codename of the content item.</param>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content item.</returns>
    [Get("/items/{codename}")]
    internal Task<IDeliveryItemResponse<T>> GetItemInternalAsync<T>(
        string codename,
        [Query] SingleItemParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    /// <summary>
    /// Gets multiple content items with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters as a dictionary.</param>
    /// <param name="waitForLoadingNewContent">Wait for loading new content header.</param>
    /// <returns>Raw JSON response containing the content items.</returns>
    [Get("/items")]
    internal Task<IDeliveryItemListingResponse<T>> GetItemsInternalAsync<T>(
        [Query] ListItemsParams? queryParameters = null,
        [Header("X-KC-Wait-For-Loading-New-Content")] bool? waitForLoadingNewContent = null);

    // ------------- Fluent, user-friendly default interface methods -------------

    /// <summary>
    /// Starts a fluent query for listing content items.
    /// Call <c>ExecuteAsync()</c> at the end to perform the request.
    /// </summary>
    public IItemsQuery<T> GetItemsAsync<T>() => new ItemsQuery<T>(this);

    /// <summary>
    /// Starts a fluent query for a single content item by codename.
    /// Call <c>ExecuteAsync()</c> at the end to perform the request.
    /// </summary>
    public IItemQuery<T> GetItemAsync<T>(string codename) => new ItemQuery<T>(this, codename);

    /// <summary>
    /// Default overload for direct execution with typed params without using fluent API.
    /// Internally forwards to the generated Refit implementation.
    /// </summary>
    public Task<IDeliveryItemListingResponse<T>> GetItemsAsync<T>(
        ListItemsParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetItemsInternalAsync<T>(queryParameters, waitForLoadingNewContent);

    /// <summary>
    /// Default overload for direct execution with typed params without using fluent API.
    /// Internally forwards to the generated Refit implementation.
    /// </summary>
    public Task<IDeliveryItemResponse<T>> GetItemAsync<T>(
        string codename,
        SingleItemParams? queryParameters = null,
        bool? waitForLoadingNewContent = null)
        => GetItemInternalAsync<T>(codename, queryParameters, waitForLoadingNewContent);
}

// ------------- Fluent query contracts -------------

/// <summary>
/// Fluent builder for listing content items.
/// </summary>
public interface IItemsQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    IItemsQuery<T> WithLanguageCodename(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IItemsQuery<T> WithElementsCodenamed(params string[] elementCodenames);
    /// <summary>
    /// Excludes specified element codenames from the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to exclude.</param>
    IItemsQuery<T> WithoutElementsCodenamed(params string[] elementCodenames);
    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    IItemsQuery<T> Depth(int depth);
    /// <summary>
    /// Sets the number of items to skip.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    IItemsQuery<T> Skip(int skip);
    /// <summary>
    /// Sets the maximum number of items to return.
    /// </summary>
    /// <param name="limit">Maximum number of items.</param>
    IItemsQuery<T> Limit(int limit);
    /// <summary>
    /// Orders the items by the given path in ascending or descending order.
    /// </summary>
    /// <param name="elementOrAttributePath">Element or attribute path.</param>
    /// <param name="ascending">True for ascending; false for descending.</param>
    IItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true);
    /// <summary>
    /// Requests the total count to be included in the response.
    /// </summary>
    IItemsQuery<T> WithTotalCount();

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery items listing response.</returns>
    Task<IDeliveryItemListingResponse<T>> ExecuteAsync();
}

/// <summary>
/// Fluent builder for retrieving a single content item by codename.
/// </summary>
public interface IItemQuery<T>
{
    /// <summary>
    /// Sets the language codename for the request.
    /// </summary>
    /// <param name="languageCodename">Language codename.</param>
    IItemQuery<T> WithLanguageCodename(string languageCodename);
    /// <summary>
    /// Includes only specified element codenames in the response.
    /// </summary>
    /// <param name="elementCodenames">Element codenames to include.</param>
    IItemQuery<T> WithElements(params string[] elementCodenames);
    /// <summary>
    /// Sets the linked items depth.
    /// </summary>
    /// <param name="depth">Depth value.</param>
    IItemQuery<T> Depth(int depth);

    /// <summary>
    /// Executes the built query.
    /// </summary>
    /// <returns>Delivery item response.</returns>
    Task<IDeliveryItemResponse<T>> ExecuteAsync();
}

// ------------- Fluent query implementations -------------

internal sealed class ItemsQuery<T> : IItemsQuery<T>
{
    private readonly IDeliveryApi _api;
    private ListItemsParams _params = new();

    public ItemsQuery(IDeliveryApi api)
    {
        _api = api;
    }

    public IItemsQuery<T> WithLanguageCodename(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IItemsQuery<T> WithElementsCodenamed(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemsQuery<T> WithoutElementsCodenamed(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IItemsQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IItemsQuery<T> Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IItemsQuery<T> Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IItemsQuery<T> WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public Task<IDeliveryItemListingResponse<T>> ExecuteAsync()
    {
        return _api.GetItemsInternalAsync<T>(_params, null);
    }
}

internal sealed class ItemQuery<T> : IItemQuery<T>
{
    private readonly IDeliveryApi _api;
    private readonly string _codename;
    private SingleItemParams _params = new();

    public ItemQuery(IDeliveryApi api, string codename)
    {
        _api = api;
        _codename = codename;
    }

    public IItemQuery<T> WithLanguageCodename(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IItemQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public Task<IDeliveryItemResponse<T>> ExecuteAsync()
    {
        return _api.GetItemInternalAsync<T>(_codename, _params, null);
    }
}

// ------------- Adapter parameter classes for Refit [Query] -------------

// Note: Adapter classes removed; fluent builders now directly produce record instances.
