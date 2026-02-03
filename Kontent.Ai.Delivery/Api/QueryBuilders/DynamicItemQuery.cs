using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.ContentItems.Mapping;

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
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper) : IDynamicItemQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private SingleItemParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IDynamicItemQuery WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
    {
        _params = _params with { Language = languageCodename };
        if (languageFallbackMode == LanguageFallbackMode.Disabled)
        {
            _serializedFilters.Add(new KeyValuePair<string, string>(
                FilterPath.System("language") + FilterSuffix.Eq,
                FilterValueSerializer.Serialize(languageCodename)));
        }
        return this;
    }

    public IDynamicItemQuery WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IDynamicItemQuery WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IDynamicItemQuery Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IDynamicItemQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public async Task<IDeliveryResult<IContentItem>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // 1. FETCH AS DYNAMIC
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var rawResponse = await _api.GetItemInternalAsync<IDynamicElements>(
            _codename,
            _params,
            FilterQueryParams.ToQueryDictionary(_serializedFilters),
            wait,
            cancellationToken).ConfigureAwait(false);
        var deliveryResult = await rawResponse.ToDeliveryResultAsync().ConfigureAwait(false);

        if (!deliveryResult.IsSuccess)
        {
            return DeliveryResult.Failure<IContentItem>(
                deliveryResult.RequestUrl ?? string.Empty,
                deliveryResult.StatusCode,
                deliveryResult.Error,
                deliveryResult.ResponseHeaders);
        }

        var response = deliveryResult.Value;
        var dynamicItem = response.Item;

        // 2. ATTEMPT RUNTIME TYPE RESOLUTION
        if (dynamicItem is IRawContentItem rawContentItem && rawContentItem.RawItemJson.HasValue)
        {
            var runtimeItem = await _contentItemMapper.TryRuntimeTypeItemAsync(
                rawContentItem.RawItemJson.Value,
                response.ModularContent,
                dependencyContext: null,
                cancellationToken).ConfigureAwait(false);

            if (runtimeItem != null)
            {
                return DeliveryResult.Success(
                    runtimeItem,
                    deliveryResult.RequestUrl ?? string.Empty,
                    deliveryResult.StatusCode,
                    deliveryResult.HasStaleContent,
                    deliveryResult.ContinuationToken,
                    deliveryResult.ResponseHeaders);
            }
        }

        // 3. FALL BACK TO DYNAMIC (no type provider mapping)
        return DeliveryResult.Success<IContentItem>(
            dynamicItem,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken,
            deliveryResult.ResponseHeaders);
    }
}
