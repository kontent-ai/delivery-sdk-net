using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.Logging;
using Kontent.Ai.Delivery.UsedIn;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemUsedInQuery"/>
internal sealed class ItemUsedInQuery(
    IDeliveryApi api,
    string codename,
    ILogger? logger = null) : IItemUsedInQuery, IUsedInQueryStatusProvider
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private readonly UsedInQueryCore _core = new(
        "ItemUsedIn",
        codename,
        api.GetItemUsedInInternalAsync,
        logger);

    public IItemUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _core.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IItemUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var filterBuilder = new ItemsFilterBuilder(_serializedFilters);
        build(filterBuilder);
        return this;
    }

    public IAsyncEnumerable<IUsedInItem> EnumerateAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateAsync(_serializedFilters, cancellationToken);

    public IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsWithStatusAsync(_serializedFilters, cancellationToken);
}

/// <inheritdoc cref="IAssetUsedInQuery"/>
internal sealed class AssetUsedInQuery(
    IDeliveryApi api,
    string codename,
    ILogger? logger = null) : IAssetUsedInQuery, IUsedInQueryStatusProvider
{
    private readonly SerializedFilterCollection _serializedFilters = [];
    private readonly UsedInQueryCore _core = new(
        "AssetUsedIn",
        codename,
        api.GetAssetUsedInInternalAsync,
        logger);

    public IAssetUsedInQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _core.WaitForLoadingNewContent(enabled);
        return this;
    }

    public IAssetUsedInQuery Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var filterBuilder = new ItemsFilterBuilder(_serializedFilters);
        build(filterBuilder);
        return this;
    }

    public IAsyncEnumerable<IUsedInItem> EnumerateAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateAsync(_serializedFilters, cancellationToken);

    public IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsWithStatusAsync(_serializedFilters, cancellationToken);
}

internal interface IUsedInQueryStatusProvider
{
    IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default);
}

internal sealed class UsedInQueryCore(
    string queryType,
    string codename,
    Func<string, Dictionary<string, string[]>?, bool?, string?, CancellationToken, Task<IApiResponse<DeliveryUsedInResponse>>> fetchPage,
    ILogger? logger)
{
    private bool _waitForLoadingNewContent;

    public void WaitForLoadingNewContent(bool enabled = true) => _waitForLoadingNewContent = enabled;

    public async IAsyncEnumerable<IUsedInItem> EnumerateAsync(
        SerializedFilterCollection filters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var pageResult in EnumerateItemsWithStatusAsync(filters, cancellationToken).ConfigureAwait(false))
        {
            if (!pageResult.IsSuccess)
            {
                yield break;
            }

            foreach (var parent in pageResult.Value)
            {
                yield return parent;
            }
        }
    }

    public async IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(
        SerializedFilterCollection filters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        var queryDictionary = filters.ToQueryDictionary();
        string? token = null;

        while (true)
        {
            var response = await fetchPage(
                codename,
                queryDictionary,
                waitForLoadingNewContent,
                token,
                cancellationToken).ConfigureAwait(false);

            var deliveryResult = await response.ToDeliveryResultAsync(logger).ConfigureAwait(false);
            if (!deliveryResult.IsSuccess)
            {
                if (logger is not null)
                {
                    LoggerMessages.PaginationStoppedEarly(logger, queryType);
                }

                yield return DeliveryResult.FailureFrom<IReadOnlyList<IUsedInItem>, DeliveryUsedInResponse>(deliveryResult);
                yield break;
            }

            IReadOnlyList<IUsedInItem> items = deliveryResult.Value.Items;
            yield return DeliveryResult.SuccessFrom(items, deliveryResult);

            token = deliveryResult.ContinuationToken;
            if (string.IsNullOrEmpty(token))
            {
                yield break;
            }
        }
    }
}
