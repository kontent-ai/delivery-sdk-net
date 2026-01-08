using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IEnumerateItemsQuery{TModel}"/>
internal sealed class EnumerateItemsQuery<TModel>(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent, IElementsPostProcessor elementsPostProcessor) : IEnumerateItemsQuery<TModel>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly List<KeyValuePair<string, string>> _serializedFilters = [];
    private static bool IsDynamicModel =>
        typeof(TModel) == typeof(Kontent.Ai.Delivery.Abstractions.IDynamicElements) ||
        typeof(TModel) == typeof(Kontent.Ai.Delivery.ContentItems.DynamicElements);

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

    public IEnumerateItemsQuery<TModel> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
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

        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;

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
                yield break;

            foreach (var item in resp.Content.Items)
            {
                // Dynamic mode intentionally stays raw (no hydration).
                if (!IsDynamicModel)
                {
                    await _elementsPostProcessor.ProcessAsync(item, null, null, cancellationToken).ConfigureAwait(false);
                }
                yield return item;
            }

            token = resp.Continuation();
            if (string.IsNullOrEmpty(token))
                yield break;
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