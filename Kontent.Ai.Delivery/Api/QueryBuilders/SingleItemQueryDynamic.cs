using System.Threading;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Extensions;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ISingleItemQueryDynamic"/>
internal sealed class SingleItemQueryDynamic(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent) : ISingleItemQueryDynamic
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private SingleItemParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public ISingleItemQueryDynamic WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public ISingleItemQueryDynamic WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ISingleItemQueryDynamic WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public ISingleItemQueryDynamic Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public ISingleItemQueryDynamic WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Get raw response from Refit API
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemInternalAsync<IElementsModel>(_codename, _params, header).ConfigureAwait(false);
        
        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync();
        
        // Map from IDeliveryItemResponse<IElementsModel> to IContentItem (non-generic)
        return deliveryResult.Map(response => (IContentItem)response.Item);
    }
}