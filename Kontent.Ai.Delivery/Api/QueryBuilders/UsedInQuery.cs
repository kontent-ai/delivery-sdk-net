using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class ItemUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IItemUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetItemUsedInInternalAsync(_codename, header, null);
    }
}

internal sealed class AssetUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IAssetUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryItemsFeedResponse<IUsedInItem>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetAssetUsedInInternalAsync(_codename, header, null);
    }
}
