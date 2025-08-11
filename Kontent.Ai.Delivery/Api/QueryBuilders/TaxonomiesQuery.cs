using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TaxonomiesQuery(IDeliveryApi api) : ITaxonomiesQuery
{
    private readonly IDeliveryApi _api = api;
    private ListTaxonomyGroupsParams _params = new();

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

    public Task<IDeliveryTaxonomyListingResponse> ExecuteAsync()
    {
        return _api.GetTaxonomiesInternalAsync(_params, null);
    }
}


