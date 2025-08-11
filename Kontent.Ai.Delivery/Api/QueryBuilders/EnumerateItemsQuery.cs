using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class EnumerateItemsQuery<T>(IDeliveryApi api) : IEnumerateItemsQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private EnumItemsParams _params = new();

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

    public Task<IDeliveryItemsFeedResponse<T>> ExecuteAsync()
    {
        return _api.GetItemsFeedInternalAsync<T>(_params, null);
    }
}
