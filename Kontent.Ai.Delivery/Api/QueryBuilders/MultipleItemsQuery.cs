using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class MultipleItemsQuery<T>(IDeliveryApi api) : IMultipleItemsQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private ListItemsParams _params = new();

    public IMultipleItemsQuery<T> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public IMultipleItemsQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public IMultipleItemsQuery<T> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public IMultipleItemsQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public IMultipleItemsQuery<T> Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public IMultipleItemsQuery<T> Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public IMultipleItemsQuery<T> OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public IMultipleItemsQuery<T> WithTotalCount()
    {
        _params = _params with { IncludeTotalCount = true };
        return this;
    }

    public Task<IDeliveryItemListingResponse<T>> ExecuteAsync()
    {
        return _api.GetItemsInternalAsync<T>(_params, null);
    }
}
