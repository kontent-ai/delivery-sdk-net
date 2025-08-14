using System;
using System.Collections.Generic;
using System.Linq;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders.Filtering;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypesQuery(IDeliveryApi api) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TypeFilters _filters = new();
    private readonly List<IFilter> _appliedFilters = [];
    private ListTypesParams _params = new();

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

    public Task<IDeliveryTypeListingResponse> ExecuteAsync()
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = _appliedFilters.Select(f => f.ToQueryParameter()).ToArray() }
            : _params;
        
        return _api.GetTypesInternalAsync(paramsWithFilters, null);
    }
}


