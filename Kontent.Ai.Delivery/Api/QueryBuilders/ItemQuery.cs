using Kontent.Ai.Delivery.Abstractions.ContentItems.Processing;
using Kontent.Ai.Delivery.Caching;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IItemQuery{TModel}"/>
internal sealed class ItemQuery<TModel>(
    IDeliveryApi api,
    string codename,
    Func<bool?> getDefaultWaitForNewContent,
    IElementsPostProcessor elementsPostProcessor,
    IDeliveryCacheManager? cacheManager) : IItemQuery<TModel>
    where TModel : IElementsModel
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private SingleItemParams _params = new();
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private readonly IDeliveryCacheManager? _cacheManager = cacheManager;
    private bool? _waitForLoadingNewContentOverride;

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

    public async Task<IDeliveryResult<IContentItem<TModel>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // ========== 1. CACHE CHECK (if enabled) ==========
        string? cacheKey = null;
        if (_cacheManager != null)
        {
            try
            {
                cacheKey = CacheKeyBuilder.BuildItemKey(_codename, _params);
                var cached = await _cacheManager.GetAsync<IDeliveryResult<IContentItem<TModel>>>(cacheKey, cancellationToken)
                    .ConfigureAwait(false);

                if (cached != null)
                {
                    return cached; // Cache hit
                }
            }
            catch (Exception)
            {
                // Cache read failed - continue with API call
                // In production, this should be logged
            }
        }

        // ========== 2. API CALL (cache miss or disabled) ==========
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
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

        // ========== 3. POST-PROCESS WITH DEPENDENCY TRACKING ==========
        var resp = deliveryResult.Value;
        var item = resp.Item;

        // Create dependency context only if caching enabled
        var dependencyContext = _cacheManager != null ? new DependencyTrackingContext() : null;

        // Track primary item dependency
        dependencyContext?.TrackItem(item.System.Codename);

        // Track modular content dependencies
        if (dependencyContext != null && resp.ModularContent != null)
        {
            foreach (var codename in resp.ModularContent.Keys)
            {
                dependencyContext.TrackItem(codename);
            }
        }

        // Post-process to hydrate IRichTextContent, assets, taxonomy (will track additional dependencies)
        await _elementsPostProcessor.ProcessAsync(item, resp.ModularContent, dependencyContext, cancellationToken)
            .ConfigureAwait(false);

        // ========== 4. BUILD RESULT ==========
        var result = DeliveryResult.Success<IContentItem<TModel>>(
            item,
            deliveryResult.RequestUrl ?? string.Empty,
            deliveryResult.StatusCode,
            deliveryResult.HasStaleContent,
            deliveryResult.ContinuationToken);

        // ========== 5. CACHE RESULT (if enabled) ==========
        if (_cacheManager != null && dependencyContext != null && cacheKey != null)
        {
            try
            {
                await _cacheManager.SetAsync(
                    cacheKey,
                    result,
                    dependencyContext.Dependencies,
                    expiration: null, // Use cache manager's default
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Cache write failed - still return result to caller
                // In production, this should be logged
            }
        }

        return result;
    }
}