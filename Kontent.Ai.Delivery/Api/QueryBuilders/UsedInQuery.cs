using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Services;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class ItemUsedInQuery(IDeliveryApi api, string codename, DeliveryResponseProcessor responseProcessor, Func<bool?> getDefaultWaitForNewContent) : IItemUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<IUsedInItem>>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var raw = await _api.GetItemUsedInInternalAsync(_codename, header, null);
        return await _responseProcessor.ProcessUsedInResponseAsync(raw);
    }
}

internal sealed class AssetUsedInQuery(IDeliveryApi api, string codename, DeliveryResponseProcessor responseProcessor, Func<bool?> getDefaultWaitForNewContent) : IAssetUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryItemsFeedResponse<IUsedInItem>>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var raw = await _api.GetAssetUsedInInternalAsync(_codename, header, null);
        return await _responseProcessor.ProcessUsedInResponseAsync(raw);
    }
}
