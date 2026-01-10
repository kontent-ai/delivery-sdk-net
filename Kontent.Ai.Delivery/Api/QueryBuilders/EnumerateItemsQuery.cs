using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;
using Kontent.Ai.Delivery.ContentItems.Mapping;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IEnumerateItemsQuery{TModel}"/>
internal sealed class EnumerateItemsQuery<TModel>(
    IDeliveryApi api,
    Func<bool?> getDefaultWaitForNewContent,
    ContentItemMapper contentItemMapper,
    ILogger? logger = null) : IEnumerateItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly ContentItemMapper _contentItemMapper = contentItemMapper;
    private readonly ILogger? _logger = logger;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private static bool IsDynamicModel =>
        typeof(TModel) == typeof(IDynamicElements) ||
        typeof(TModel) == typeof(ContentItems.DynamicElements);

    public IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename, LanguageFallbackMode languageFallbackMode = LanguageFallbackMode.Enabled)
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

    public IEnumerateItemsQuery<TModel> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IEnumerateItemsQuery<TModel> OrderBy(string elementOrAttributePath, OrderingMode orderingMode = OrderingMode.Ascending)
    {
        _params = _params with
        {
            OrderBy = orderingMode == OrderingMode.Ascending
                ? $"{elementOrAttributePath}[asc]"
                : $"{elementOrAttributePath}[desc]"
        };
        return this;
    }

    public IEnumerateItemsQuery<TModel> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public IEnumerateItemsQuery<TModel> Where(Func<IItemsFilterBuilder, IItemsFilterBuilder> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        build(new ItemsFilterBuilder(_serializedFilters));
        return this;
    }

    public async IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Log pagination start
        if (_logger != null)
            LoggerMessages.PaginationStarted(_logger, "ItemsFeed");

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;
        int pageCount = 0;
        int totalItems = 0;

        while (true)
        {
            var resp = await _api
                .GetItemsFeedInternalAsync<TModel>(
                    _params,
                    FilterQueryParams.ToQueryDictionary(_serializedFilters),
                    token,
                    wait,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode || resp.Content is null)
            {
                if (_logger != null)
                    LoggerMessages.PaginationStoppedEarly(_logger, "ItemsFeed");
                yield break;
            }

            pageCount++;

            foreach (var item in resp.Content.Items)
            {
                // Dynamic mode intentionally stays raw (no hydration).
                if (!IsDynamicModel)
                {
                    await _contentItemMapper.CompleteItemAsync(item, resp.Content.ModularContent, null, cancellationToken).ConfigureAwait(false);
                }
                totalItems++;
                yield return item;
            }

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
            {
                if (_logger != null)
                    LoggerMessages.PaginationCompleted(_logger, "ItemsFeed", pageCount, totalItems);
                yield break;
            }
        }
    }

    public async Task<IReadOnlyList<IContentItem<TModel>>> EnumerateAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IContentItem<TModel>>();
        await foreach (var item in EnumerateItemsAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results.AsReadOnly();
    }
}