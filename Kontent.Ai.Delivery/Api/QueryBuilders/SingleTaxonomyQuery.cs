using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TaxonomyQuery(IDeliveryApi api, string codename) : ITaxonomyQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;

    public Task<IDeliveryTaxonomyResponse> ExecuteAsync()
    {
        return _api.GetTaxonomyInternalAsync(_codename, null);
    }
}
