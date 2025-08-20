using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class EnumerateItemsQuery<T>(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : IEnumerateItemsQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private EnumItemsParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

    public IEnumerateItemsQuery<T> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IEnumerateItemsQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IEnumerateItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IEnumerateItemsQuery<T> WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryItemsFeedResponse<T>> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetItemsFeedInternalAsync(_params, header);
    }
}
