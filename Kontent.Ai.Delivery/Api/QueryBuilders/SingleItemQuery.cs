using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class SingleItemQuery<T>(IDeliveryApi api, string codename) : ISingleItemQuery<T>
{
    private readonly IDeliveryApi _api = api;
    private readonly string _codename = codename;
    private SingleItemParams _params = new();

    public ISingleItemQuery<T> WithLanguage(string languageCodename)
    {
        _params = _params with { Language = languageCodename };
        return this;
    }

    public ISingleItemQuery<T> WithElements(params string[] elementCodenames)
    {
        _params = _params with { Elements = elementCodenames };
        return this;
    }

    public ISingleItemQuery<T> WithoutElements(params string[] elementCodenames)
    {
        _params = _params with { ExcludeElements = elementCodenames };
        return this;
    }

    public ISingleItemQuery<T> Depth(int depth)
    {
        _params = _params with { Depth = depth };
        return this;
    }

    public Task<IDeliveryItemResponse<T>> ExecuteAsync()
    {
        return _api.GetItemInternalAsync<T>(_codename, _params, null);
    }
}