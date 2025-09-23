using System.Threading;
using System.Runtime.CompilerServices;
using Kontent.Ai.Delivery.Api.QueryBuilders.Filtering;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="IEnumerateItemsQuery{TModel}"/>
internal sealed class EnumerateItemsQuery<TModel>(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent, IElementsPostProcessor elementsPostProcessor) : IEnumerateItemsQuery<TModel>
    where TModel : IElementsModel
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private readonly IElementsPostProcessor _elementsPostProcessor = elementsPostProcessor;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;
    private readonly ItemFilters _filters = new();
    private readonly Dictionary<string, string> _serializedFilters = [];

    public IEnumerateItemsQuery<TModel> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
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

    public IEnumerateItemsQuery<TModel> Filter(Func<IItemFilters, IFilter> filterBuilder)
    {
        var filter = filterBuilder(_filters);
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public IEnumerateItemsQuery<TModel> Where(IFilter filter)
    {
        var (key, value) = filter.ToQueryParameter();
        _serializedFilters.Add(key, value);
        return this;
    }

    public async IAsyncEnumerable<IContentItem<TModel>> EnumerateItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
            
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        string? token = null;

        while (true)
        {
            var resp = await _api
                .GetItemsFeedInternalAsync<TModel>(_params, _serializedFilters, token, wait, cancellationToken)
                .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode || resp.Content is null)
                yield break;

            foreach (var item in resp.Content.Items)
            {
                await _elementsPostProcessor.ProcessAsync(item, null, cancellationToken).ConfigureAwait(false);
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
