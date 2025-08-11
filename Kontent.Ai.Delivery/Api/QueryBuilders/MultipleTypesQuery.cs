using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypesQuery(IDeliveryApi api) : ITypesQuery
{
    private readonly IDeliveryApi _api = api;
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

    public Task<IDeliveryTypeListingResponse> ExecuteAsync()
    {
        return _api.GetTypesInternalAsync(_params, null);
    }
}


