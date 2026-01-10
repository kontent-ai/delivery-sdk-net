using System.Net;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems;
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
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IDynamicItemsQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _params = _params with { Language = languageCodename };
        if (languageFallbackMode == LanguageFallbackMode.Disabled)
        {
            _serializedFilters.Add(new KeyValuePair<string, string>(
                FilterPath.System("language") + FilterSuffix.Eq,
                FilterValueSerializer.Serialize(languageCodename)));
        }
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

    public IDynamicItemsQuery OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
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

    public IDynamicItemsQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemListingResponse<IDynamicElements>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Get raw response from Refit API
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<IDynamicElements>(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryItemListingResponse<IDynamicElements>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // Add next page fetcher to the response
        var resp = deliveryResult.Value;
        var responseWithFetcher = resp with
        {
            NextPageFetcher = CreateNextPageFetcher(resp.Pagination)
        };

        return DeliveryResult.Success<IDeliveryItemListingResponse<IDynamicElements>>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<IDynamicElements>>>>? CreateNextPageFetcher(IPagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl)) // TODO: why calculate nest skip value when it's returned by the API?
            return null;

        // Calculate next skip value
        var nextSkip = pagination.Skip + pagination.Count;

        return async (ct) =>
        {
            // Create a new query with updated skip
            var nextQuery = new DynamicItemsQuery(_api, _getDefaultWaitForNewContent)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            // Copy filters
            foreach (var filter in _serializedFilters)
            {
                nextQuery._serializedFilters.Add(filter);
            }

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    public async Task<IDeliveryResult<IDeliveryItemListingResponse<IDynamicElements>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<ContentItem<IDynamicElements>>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;
        string? requestUrl = null;
        System.Net.Http.Headers.HttpResponseHeaders? responseHeaders = null;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit };

            var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var response = await _api.GetItemsInternalAsync<IDynamicElements>(
                pageParams,
                FilterQueryParams.ToQueryDictionary(_serializedFilters),
                wait).ConfigureAwait(false);

            // Convert to delivery result
            var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

            if (!deliveryResult.IsSuccess)
            {
                return DeliveryResult.Failure<IDeliveryItemListingResponse<IDynamicElements>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error!,
                    deliveryResult.ResponseHeaders);
            }

            // Capture request URL and headers from first request
            requestUrl ??= deliveryResult.RequestUrl;
            responseHeaders ??= deliveryResult.ResponseHeaders;

            var items = deliveryResult.Value.Items;
            var pageCount = items.Count;

            if (pageCount == 0)
                break;

            all.AddRange(items);
            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted)
            if (limit.HasValue && pageCount < limit.Value)
                break;
        }

        // Create a synthetic response with all items
        var allItemsResponse = new DeliveryItemListingResponse<IDynamicElements>
        {
            Items = all,
            Pagination = new Pagination
            {
                Skip = _params.Skip ?? 0,
                Limit = all.Count,
                Count = all.Count,
                NextPageUrl = null,
                TotalCount = all.Count
            },
            ModularContent = [],
            NextPageFetcher = null
        };

        return DeliveryResult.Success<IDeliveryItemListingResponse<IDynamicElements>>(
            allItemsResponse,
            requestUrl ?? string.Empty,
            HttpStatusCode.OK,
            hasStaleContent: false,
            continuationToken: null,
            responseHeaders: responseHeaders);
    }
}