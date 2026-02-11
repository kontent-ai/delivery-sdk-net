using System.Runtime.CompilerServices;
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

    public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsAsync(cancellationToken);

    public IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsWithStatusAsync(cancellationToken);
}

/// <inheritdoc cref="IAssetUsedInQuery"/>
internal sealed class AssetUsedInQuery(
    IDeliveryApi api,
    string codename,
    ILogger? logger = null) : IAssetUsedInQuery, IUsedInQueryStatusProvider
{
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

    public IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsAsync(cancellationToken);

    public IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default)
        => _core.EnumerateItemsWithStatusAsync(cancellationToken);
}

internal interface IUsedInQueryStatusProvider
{
    IAsyncEnumerable<IDeliveryResult<IReadOnlyList<IUsedInItem>>> EnumerateItemsWithStatusAsync(CancellationToken cancellationToken = default);
}

internal sealed class UsedInQueryCore(
    string queryType,
    string codename,
    Func<string, bool?, string?, CancellationToken, Task<IApiResponse<DeliveryUsedInResponse>>> fetchPage,
    ILogger? logger)
{
    private readonly string _queryType = queryType;
    private readonly string _codename = codename;
    private readonly Func<string, bool?, string?, CancellationToken, Task<IApiResponse<DeliveryUsedInResponse>>> _fetchPage = fetchPage;
    private readonly ILogger? _logger = logger;
    private bool _waitForLoadingNewContent;

    public void WaitForLoadingNewContent(bool enabled = true) => _waitForLoadingNewContent = enabled;

    public async IAsyncEnumerable<IUsedInItem> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var pageResult in EnumerateItemsWithStatusAsync(cancellationToken).ConfigureAwait(false))
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool? waitForLoadingNewContent = _waitForLoadingNewContent ? true : null;
        string? token = null;

        while (true)
        {
            var response = await _fetchPage(
                _codename,
                waitForLoadingNewContent,
                token,
                cancellationToken).ConfigureAwait(false);

            var deliveryResult = await response.ToDeliveryResultAsync(_logger).ConfigureAwait(false);
            if (!deliveryResult.IsSuccess)
            {
                if (_logger is not null)
                {
                    LoggerMessages.PaginationStoppedEarly(_logger, _queryType);
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
