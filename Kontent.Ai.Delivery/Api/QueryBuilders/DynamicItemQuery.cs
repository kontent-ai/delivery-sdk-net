using System.Threading;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.ContentItems;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IDynamicItemQuery"/>
internal sealed class DynamicItemQuery(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent) : IDynamicItemQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private SingleItemParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IDynamicItemQuery WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IDynamicItemQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IDynamicItemQuery WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IDynamicItemQuery Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IDynamicItemQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<IElementsModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Get raw response from Refit API
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemInternalAsync<IElementsModel>(_codename, _params, wait).ConfigureAwait(false);
        
        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync();
        
        // Map from IDeliveryItemResponse<IElementsModel> to IContentItem<DynamicElements>
        return deliveryResult.Map(response => response.Item);
    }
}