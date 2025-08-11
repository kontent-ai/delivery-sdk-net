using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypeElementQuery(IDeliveryApi api, string contentTypeCodename, string elementCodename) : ITypeElementQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _type = contentTypeCodename;
    private readonly string _element = elementCodename;

    public Task<IDeliveryElementResponse> ExecuteAsync()
    {
        return _api.GetContentElementInternalAsync(_type, _element, null);
    }
}


