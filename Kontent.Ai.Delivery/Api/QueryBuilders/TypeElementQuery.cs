using System.Threading;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeElementQuery"/>
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

    public async Task<IDeliveryResult<IContentElement>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetContentElementInternalAsync(_type, _element, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Element);
    }
}


