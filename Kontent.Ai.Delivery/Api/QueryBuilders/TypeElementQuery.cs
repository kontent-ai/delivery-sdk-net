
namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ITypeElementQuery"/>
internal sealed class TypeElementQuery(IDeliveryApi api, string contentTypeCodename, string elementCodename) : ITypeElementQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _type = contentTypeCodename;
    private readonly string _element = elementCodename;
    private bool _waitForLoadingNewContent;

    public ITypeElementQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContent = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentElement>> ExecuteAsync(CancellationToken cancellationToken = default)
        => await FetchFromApiAsync(cancellationToken).ConfigureAwait(false);

    private async Task<IDeliveryResult<IContentElement>> FetchFromApiAsync(CancellationToken cancellationToken)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var response = await _api.GetContentElementInternalAsync(_type, _element, waitForLoadingNewContent, cancellationToken).ConfigureAwait(false);
        return await response.ToDeliveryResultAsync().ConfigureAwait(false);
    }
}
