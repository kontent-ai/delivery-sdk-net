using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IDynamicItemQuery"/>
/// <remarks>
/// This query performs runtime type resolution for dynamic items. Cache support is intentionally
/// omitted as the runtime-typed result type varies per item, making caching complex.
/// Use strongly-typed queries (<see cref="IItemQuery{TModel}"/>) for cacheable results.
/// </remarks>
internal sealed class DynamicItemQuery(
    IDeliveryApi api,
    string codename,
    ContentItemMapper contentItemMapper,
    IContentDeserializer contentDeserializer,
    string? defaultRenditionPreset = null,
    ILogger? logger = null) : IDynamicItemQuery
{
    private readonly ItemQuery<IDynamicElements> _inner = new(
        api,
        codename,
        contentItemMapper,
        contentDeserializer,
        cacheManager: null,
        defaultRenditionPreset,
        logger);

    public IDynamicItemQuery WithLanguage(string languageCodename)
    {
        _inner.WithLanguage(languageCodename);
        return this;
    }

    public IDynamicItemQuery WithElements(params string[] elementCodenames)
    {
        _inner.WithElements(elementCodenames);
        return this;
    }

    public IDynamicItemQuery WithoutElements(params string[] elementCodenames)
    {
        _inner.WithoutElements(elementCodenames);
        return this;
    }

    public IDynamicItemQuery Depth(int depth)
    {
        _inner.Depth(depth);
        return this;
    }

    public IDynamicItemQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _inner.WaitForLoadingNewContent(enabled);
        return this;
    }

    public async Task<IDeliveryResult<IContentItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var deliveryResult = await _inner.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return (IDeliveryResult<IContentItem>)deliveryResult;
        }

        var dynamicItem = deliveryResult.Value;

        if (dynamicItem is IRawContentItem rawContentItem && rawContentItem.RawItemJson.HasValue)
        {
            var runtimeItem = await contentItemMapper.TryRuntimeTypeItemAsync(
                rawContentItem.RawItemJson.Value,
                _inner.LatestModularContent,
                dependencyContext: null,
                defaultRenditionPreset,
                cancellationToken).ConfigureAwait(false);

            if (runtimeItem is not null)
            {
                return DeliveryResult.SuccessFrom(runtimeItem, deliveryResult);
            }
        }

        return deliveryResult;
    }
}
