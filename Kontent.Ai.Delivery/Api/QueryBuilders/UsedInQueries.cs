using System.Threading;
using Kontent.Ai.Delivery.Abstractions.QueryBuilders;
using Kontent.Ai.Delivery.Abstractions.SharedModels;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

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

    public async Task<IDeliveryResult<IReadOnlyList<IUsedInItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetItemUsedInInternalAsync(_codename, header, null, cancellationToken).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Items.AsReadOnly());
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

    public async Task<IReadOnlyList<IUsedInItem>> EnumerateAllItemsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IUsedInItem>();
        await foreach (var item in EnumerateItemsAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results.AsReadOnly();
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

    public async Task<IDeliveryResult<IReadOnlyList<IUsedInItem>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetAssetUsedInInternalAsync(_codename, wait, null, cancellationToken).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Items.AsReadOnly());
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

    public async Task<IReadOnlyList<IUsedInItem>> EnumerateAllItemsAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IUsedInItem>();
        await foreach (var item in EnumerateItemsAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results.AsReadOnly();
    }
}
