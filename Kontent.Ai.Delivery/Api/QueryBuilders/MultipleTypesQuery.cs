using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypesQuery(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TypeFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListTypesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITypesQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ITypesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ITypesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public ITypesQuery Where(Func<ITypeFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        _appliedFilters.Add(filter);
        return this;
    }

    public ITypesQuery Where(IFilter filter)
    {
        _appliedFilters.Add(filter);
        return this;
    }

    public ITypesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryTypeListingResponse> ExecuteAsync()
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = _appliedFilters.Select(f => f.ToQueryParameter()).ToArray() }
            : _params;
        
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetTypesInternalAsync(paramsWithFilters, header);
    }
}


