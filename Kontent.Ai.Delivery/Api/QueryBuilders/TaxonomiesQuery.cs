using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TaxonomiesQuery(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TaxonomyFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListTaxonomyGroupsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITaxonomiesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITaxonomiesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITaxonomiesQuery Where(Func<ITaxonomyFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        _appliedFilters.Add(filter);
        return this;
    }

    public ITaxonomiesQuery Where(IFilter filter)
    {
        _appliedFilters.Add(filter);
        return this;
    }

    public ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryTaxonomyListingResponse> ExecuteAsync()
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = _appliedFilters.Select(f => f.ToQueryParameter()).ToArray() }
            : _params;
        
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetTaxonomiesInternalAsync(paramsWithFilters, header);
    }
}


