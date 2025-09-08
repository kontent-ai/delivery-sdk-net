using System.Threading;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Extensions;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <summary>
/// Concrete implementation of <see cref="IDynamicItemsQuery"/> using the modernized Result pattern.
/// </summary>
internal sealed class DynamicItemsQuery(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent) : IDynamicItemsQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ItemFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IDynamicItemsQuery WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IDynamicItemsQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IDynamicItemsQuery WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IDynamicItemsQuery Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IDynamicItemsQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IDynamicItemsQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IDynamicItemsQuery OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IDynamicItemsQuery WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public IDynamicItemsQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IDynamicItemsQuery Filter(Func<IItemFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        _appliedFilters.Add(filter);
        return this;
    }

    public IDynamicItemsQuery Where(IFilter filter)
    {
        _appliedFilters.Add(filter);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = [.. _appliedFilters.Select(f => f.ToQueryParameter())] }
            : _params;
        
        // Get raw response from Refit API
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<IElementsModel>(paramsWithFilters, header).ConfigureAwait(false);
        
        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync();
        
        // Map from IDeliveryItemListingResponse<IElementsModel> to IReadOnlyList<IContentItem>
        return deliveryResult.Map(response => (IReadOnlyList<IContentItem>)response.Items.Cast<IContentItem>().ToList().AsReadOnly());
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<IContentItem>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;
        string? requestUrl = null;

        // Build filters once; avoid per-iteration allocations.
        var filters = _appliedFilters.Count > 0
            ? _appliedFilters.Select(f => f.ToQueryParameter()).ToArray()
            : _params.Filters;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit, Filters = filters };

            var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var response = await _api.GetItemsInternalAsync<IElementsModel>(pageParams, wait).ConfigureAwait(false);

            // Convert to delivery result
            var deliveryResult = await response.ToDeliveryResultAsync();
            
            if (!deliveryResult.IsSuccess)
            {
                return DeliveryResult.Failure<IReadOnlyList<IContentItem>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error!);
            }

            // Capture request URL from first request
            requestUrl ??= deliveryResult.RequestUrl;

            var items = deliveryResult.Value.Items.Cast<IContentItem>().ToList();
            var pageCount = items.Count;

            if (pageCount == 0)
                break;

            all.AddRange(items);
            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted)
            if (limit.HasValue && pageCount < limit.Value)
                break;
        }

        return DeliveryResult.Success<IReadOnlyList<IContentItem>>(
            all.AsReadOnly(),
            requestUrl ?? string.Empty,
            200,
            hasStaleContent: false,
            continuationToken: null);
    }
}