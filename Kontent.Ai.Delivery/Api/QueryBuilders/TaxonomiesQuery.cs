using System.Threading;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITaxonomiesQuery"/>
internal sealed class TaxonomiesQuery(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly TaxonomyFilters _filters = new();
    private readonly Dictionary<string, string> _serializedFilters = [];
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
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITaxonomiesQuery Where(IFilter filter)
    {
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public ITaxonomiesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IReadOnlyList<ITaxonomyGroup>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetTaxonomiesInternalAsync(_params, _serializedFilters, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Taxonomies.AsReadOnly());
    }
}


