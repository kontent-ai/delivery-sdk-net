using System.Runtime.CompilerServices;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemUsedInQuery"/>
internal sealed class ItemUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IItemUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        while (true)
        {
            var resp = await _api.GetItemUsedInInternalAsync(_codename, wait, token, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode || resp.Content is null)
                yield break;

            foreach (var parent in resp.Content.Items)
                yield return parent;

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
                yield break;
        }
    }
}

/// <inheritdoc cref="IAssetUsedInQuery"/>
internal sealed class AssetUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IAssetUsedInQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private bool? _waitForLoadingNewContentOverride;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;

    public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        while (true)
        {
            var resp = await _api.GetAssetUsedInInternalAsync(_codename, wait, token, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode || resp.Content is null)
                yield break;

            foreach (var parent in resp.Content.Items)
                yield return parent;

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
                yield break;
        }
    }
}
