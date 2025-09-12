using System.Threading;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemQuery{TModel}"/>
internal sealed class ItemQuery<TModel>(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    Func<bool> getDefaultRenderRichTextToHtml,
    IElementsPostProcessor elementsPostProcessor) : IItemQuery<TModel>
    where TModel : IElementsModel
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly Func<bool> _getDefaultRenderRichTextToHtml = getDefaultRenderRichTextToHtml;
    private SingleItemParams _params = new();
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private bool? _waitForLoadingNewContentOverride;
    private bool? _renderRichTextToHtmlOverride;

    public IItemQuery<TModel> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IItemQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IItemQuery<TModel> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IItemQuery<TModel> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IItemQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IItemQuery<TModel> RenderRichTextToHtml(bool render = true)
    {
        _renderRichTextToHtmlOverride = render;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        // The renderRichText flag is carried for downstream processing (mapping). API call remains unchanged.
        var _ = _renderRichTextToHtmlOverride ?? _getDefaultRenderRichTextToHtml();
        var rawResponse = await _api.GetItemInternalAsync<TModel>(_codename, _params, wait).ConfigureAwait(false);

        // Convert IApiResponse to IDeliveryResult
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IContentItem<TModel>>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error);
        }

        var resp = deliveryResult.Value;
        var item = resp.Item;

        // Post-process to hydrate IRichTextContent, etc.
        await _elementsPostProcessor.ProcessAsync(item, resp.ModularContent, cancellationToken).ConfigureAwait(false);

        return DeliveryResult.Success<IContentItem<TModel>>(
            item,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken);
    }
}