using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TaxonomyQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : ITaxonomyQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryTaxonomyResponse> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetTaxonomyInternalAsync(_codename, header);
    }
}
