using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypeElementQuery(IDeliveryApi api, string contentTypeCodename, string elementCodename, Func<bool?> getDefaultWaitForNewContent) : ITypeElementQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _type = contentTypeCodename;
    private readonly string _element = elementCodename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryElementResponse> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetContentElementInternalAsync(_type, _element, header);
    }
}


