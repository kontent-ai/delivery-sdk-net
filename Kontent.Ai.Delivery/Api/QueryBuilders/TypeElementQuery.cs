using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.ContentTypes;
using Kontent.Ai.Delivery.Serialization;
using Kontent.Ai.Delivery.SharedModels;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypeElementQuery(IDeliveryApi api, string contentTypeCodename, string elementCodename, DeliveryResponseProcessor responseProcessor, Func<bool?> getDefaultWaitForNewContent) : ITypeElementQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _type = contentTypeCodename;
    private readonly string _element = elementCodename;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryElementResponse>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var raw = await _api.GetContentElementInternalAsync(_type, _element, header);
        return await _responseProcessor.ProcessContentElementResponseAsync(raw);
    }
}


