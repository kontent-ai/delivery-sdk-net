using System.Threading;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemsQuery{TModel}"/>
internal sealed class ItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    Func<bool> getDefaultRenderRichTextToHtml,
    IElementsPostProcessor elementsPostProcessor) : IItemsQuery<TModel> where TModel : IElementsModel
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly Func<bool> _getDefaultRenderRichTextToHtml = getDefaultRenderRichTextToHtml;
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private readonly ItemFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private bool? _renderRichTextToHtmlOverride;

    public IItemsQuery<TModel> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IItemsQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemsQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IItemsQuery<TModel> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IItemsQuery<TModel> Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IItemsQuery<TModel> Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IItemsQuery<TModel> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IItemsQuery<TModel> WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public IItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IItemsQuery<TModel> RenderRichTextToHtml(bool render = true)
    {
        _renderRichTextToHtmlOverride = render;
        return this;
    }

    public IItemsQuery<TModel> Filter(Func<IItemFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        _appliedFilters.Add(filter);
        return this;
    }

    public IItemsQuery<TModel> Where(IFilter filter)
    {
        _appliedFilters.Add(filter);
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = [.. _appliedFilters.Select(f => f.ToQueryParameter())] }
            : _params;

        // Get raw response from Refit API
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var _ = _renderRichTextToHtmlOverride ?? _getDefaultRenderRichTextToHtml();
        var rawResponse = await _api.GetItemsInternalAsync<TModel>(paramsWithFilters, header).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IReadOnlyList<IContentItem<TModel>>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error);
        }

        var resp = deliveryResult.Value;
        var items = resp.Items;
        foreach (var item in items)
        {
            await _elementsPostProcessor.ProcessAsync(item, resp.ModularContent, cancellationToken).ConfigureAwait(false);
        }

        return DeliveryResult.Success<IReadOnlyList<IContentItem<TModel>>>(
            (IReadOnlyList<IContentItem<TModel>>)items,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken);
    }

    public async Task<IDeliveryResult<IReadOnlyList<IContentItem<TModel>>>> ExecuteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<IContentItem<TModel>>();
        var skip = _params.Skip ?? 0;
        var limit = _params.Limit;
        string? requestUrl = null;

        // Build filters once; avoid per-iteration allocations.
        var filters = _appliedFilters.Count > 0
            ? [.. _appliedFilters.Select(f => f.ToQueryParameter())]
            : _params.Filters;

        while (true)
        {
            var pageParams = _params with { Skip = skip, Limit = limit, Filters = filters };

            var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
            var response = await _api.GetItemsInternalAsync<TModel>(pageParams, wait).ConfigureAwait(false);

            // Convert to delivery result
            var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

            if (!deliveryResult.IsSuccess)
            {
                return DeliveryResult.Failure<IReadOnlyList<IContentItem<TModel>>>(
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.Error!);
            }

            // Capture request URL from first request
            requestUrl ??= deliveryResult.RequestUrl;

            var items = deliveryResult.Value.Items;
            var pageCount = items?.Count ?? 0;

            if (pageCount == 0)
                break;

            if (items is { Count: > 0 })
            {
                foreach (var item in items)
                {
                    await _elementsPostProcessor.ProcessAsync(item, deliveryResult.Value.ModularContent, cancellationToken).ConfigureAwait(false);
                    all.Add(item);
                }
            }

            skip += pageCount;

            // Stop if we got fewer than requested (page exhausted)
            if (limit.HasValue && pageCount < limit.Value)
                break;
        }

        return DeliveryResult.Success<IReadOnlyList<IContentItem<TModel>>>(
            all.AsReadOnly(),
            requestUrl ?? string.Empty,
            200,
            hasStaleContent: false,
            continuationToken: null);
    }
}
