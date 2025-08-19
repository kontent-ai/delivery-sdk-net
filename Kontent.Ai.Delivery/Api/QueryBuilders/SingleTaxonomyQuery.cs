using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using Kontent.Ai.Delivery.Serialization;
using Kontent.Ai.Delivery.SharedModels;
using Kontent.Ai.Delivery.TaxonomyGroups;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class TaxonomyQuery(IDeliveryApi api, string codename, DeliveryResponseProcessor responseProcessor, Func<bool?> getDefaultWaitForNewContent) : ITaxonomyQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly DeliveryResponseProcessor _responseProcessor = responseProcessor;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public ITaxonomyQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IDeliveryTaxonomyResponse>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var raw = await _api.GetTaxonomyInternalAsync(_codename, header);
        return await _responseProcessor.ProcessTaxonomyResponseAsync(raw);
    }
}
