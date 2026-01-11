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
        // API CALL
        var deliveryResult = await FetchFromApiAsync().ConfigureAwait(false);
        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IDeliveryItemListingResponse<IDynamicElements>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        // BUILD RESULT WITH NEXT PAGE FETCHER
        var resp = deliveryResult.Value;
        var responseWithFetcher = resp with { NextPageFetcher = CreateNextPageFetcher(resp.Pagination) };

        return DeliveryResult.Success<IDeliveryItemListingResponse<IDynamicElements>>(
            responseWithFetcher,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }

    private async Task<IDeliveryResult<DeliveryItemListingResponse<IDynamicElements>>> FetchFromApiAsync()
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemsInternalAsync<IDynamicElements>(
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait).ConfigureAwait(false);
        return await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);
    }

    private Func<CancellationToken, Task<IDeliveryResult<IDeliveryItemListingResponse<IDynamicElements>>>>? CreateNextPageFetcher(Pagination pagination)
    {
        if (string.IsNullOrEmpty(pagination.NextPageUrl))
            return null;

        var nextSkip = ExtractSkipFromUrl(pagination.NextPageUrl);

        return async (ct) =>
        {
            var nextQuery = new DynamicItemsQuery(_api, _getDefaultWaitForNewContent)
            {
                _params = _params with { Skip = nextSkip },
                _waitForLoadingNewContentOverride = _waitForLoadingNewContentOverride
            };

            foreach (var filter in _serializedFilters)
                nextQuery._serializedFilters.Add(filter);

            return await nextQuery.ExecuteAsync(ct).ConfigureAwait(false);
        };
    }

    private static int ExtractSkipFromUrl(string url)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return int.TryParse(query["skip"], out var skip) ? skip : 0;
    }
}