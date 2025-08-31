using System.Threading;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <summary>
/// Concrete implementation of <see cref="IMultipleItemsQuery{T}"/> using the modernized Result pattern.
/// </summary>
/// <typeparam name="T">The type of the content items.</typeparam>
internal sealed class MultipleItemsQuery<T>(
    IDeliveryApi api,
    DeliveryResponseProcessor responseProcessor,
    Func<bool?> getDefaultWaitForNewContent) : IMultipleItemsQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ItemFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IMultipleItemsQuery<T> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IMultipleItemsQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IMultipleItemsQuery<T> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IMultipleItemsQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IMultipleItemsQuery<T> Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IMultipleItemsQuery<T> Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IMultipleItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IMultipleItemsQuery<T> WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public IMultipleItemsQuery<T> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IMultipleItemsQuery<T> Filter(Func<IItemFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        _appliedFilters.Add(filter);
        return this;
    }

    public IMultipleItemsQuery<T> Where(IFilter filter)
    {
        _appliedFilters.Add(filter);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<T>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = [.. _appliedFilters.Select(f => f.ToQueryParameter())] }
            : _params;
        
        // Get raw response from Refit API
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync(paramsWithFilters, header);
        
        // Process the response to create strongly-typed result
        var processedResponse = await _responseProcessor.ProcessItemListingResponseAsync<T>(rawResponse);
        
        // Extract the content items from the processed response
        if (processedResponse.IsSuccess)
        {
            return DeliveryResult.Success<IReadOnlyList<T>>(
                processedResponse.Value.Items.ToList().AsReadOnly(),
                processedResponse.StatusCode,
                processedResponse.HasStaleContent,
                processedResponse.ContinuationToken,
                processedResponse.RequestUrl,
                processedResponse.RateLimit);
        }

        // Return error result
        return DeliveryResult.Failure<IReadOnlyList<T>>(
            processedResponse.Errors,
            processedResponse.StatusCode,
            processedResponse.RequestUrl,
            processedResponse.RateLimit);
    }

    public async Task<IDeliveryResult<IReadOnlyList<T>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<T>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;

        // Build filters once; avoid per-iteration allocations.
        var filters = _appliedFilters.Count > 0
            ? _appliedFilters.Select(f => f.ToQueryParameter()).ToArray()
            : _params.Filters;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit, Filters = filters };

            var header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var raw = await _api.GetItemsInternalAsync(pageParams, header);
            var processed = await _responseProcessor.ProcessItemListingResponseAsync<T>(raw);

            if (!processed.IsSuccess)
            {
                return DeliveryResult.Failure<IReadOnlyList<T>>(
                    processed.Errors,
                    processed.StatusCode,
                    processed.RequestUrl,
                    processed.RateLimit);
            }

            var items = processed.Value.Items;
            var pageCount = items?.Count ?? processed.Value.Pagination?.Count ?? 0;

            if (pageCount == 0)
                break;

            if (items is { Count: > 0 })
                all.AddRange(items);

            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted),
            // or if server says there is no next page (works for both with/without limit).
            var nextPageMissing = processed.Value.Pagination?.NextPageUrl is null;
            if ((limit.HasValue && pageCount < limit.Value) || nextPageMissing)
                break;
        }

        return DeliveryResult.Success<IReadOnlyList<T>>(
            all.AsReadOnly(),
            200,
            hasStaleContent: false,
            continuationToken: null,
            requestUrl: null,
            rateLimit: null);
    }
}
