using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TypeQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : ITypeQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private SingleTypeParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITypeQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ITypeQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryTypeResponse> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetTypeInternalAsync(_codename, _params, header);
    }
}


