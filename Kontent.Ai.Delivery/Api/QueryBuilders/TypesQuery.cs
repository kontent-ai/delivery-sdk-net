using System.Threading;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypesQuery"/>
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

    public async Task<IDeliveryResult<IReadOnlyList<IContentType>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var paramsWithFilters = _appliedFilters.Count > 0
            ? _params with { Filters = [.. _appliedFilters.Select(f => f.ToQueryParameter())] }
            : _params;

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTypesInternalAsync(paramsWithFilters, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Types.AsReadOnly());
    }
}


