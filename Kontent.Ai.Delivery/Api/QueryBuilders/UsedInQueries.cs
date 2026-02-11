using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.UsedIn;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemUsedInQuery"/>
internal sealed class ItemUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IItemUsedInQuery
{
    private readonly UsedInQueryCore _core = new(
        codename,
        getDefaultWaitForNewContent,
        api.GetItemUsedInInternalAsync);

    public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _core.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsAsync(cancellationToken);
}

/// <inheritdoc cref="IAssetUsedInQuery"/>
internal sealed class AssetUsedInQuery(IDeliveryApi api, string codename, Func<bool?> getDefaultWaitForNewContent) : IAssetUsedInQuery
{
    private readonly UsedInQueryCore _core = new(
        codename,
        getDefaultWaitForNewContent,
        api.GetAssetUsedInInternalAsync);

    public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _core.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsAsync(cancellationToken);
}

internal sealed class UsedInQueryCore(
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    Func<string, bool?, string?, CancellationToken, Task<IApiResponse<DeliveryUsedInResponse>>> fetchPage)
{
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly Func<string, bool?, string?, CancellationToken, Task<IApiResponse<DeliveryUsedInResponse>>> _fetchPage = fetchPage;
    private bool? _waitForLoadingNewContentOverride;

    public void WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
    }

    public async IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;

        while (true)
        {
            var resp = await _fetchPage(_codename, wait, token, cancellationToken).ConfigureAwait(false);
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
