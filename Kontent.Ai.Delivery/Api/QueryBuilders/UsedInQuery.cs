using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class ItemUsedInQuery(IDeliveryApi api, string codename) : IItemUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;

    public Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync()
    {
        return _api.GetItemUsedInInternalAsync(_codename, null, null);
    }
}

internal sealed class AssetUsedInQuery(IDeliveryApi api, string codename) : IAssetUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;

    public Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync()
    {
        return _api.GetAssetUsedInInternalAsync(_codename, null, null);
    }
}
