using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class LanguagesQuery(IDeliveryApi api) : ILanguagesQuery
{
    private readonly IDeliveryApi _api = api;
    private LanguagesParams _params = new();

    public ILanguagesQuery OrderBy(string elementOrAttributePath, bool ascending = true)
    {
        _params = _params with { OrderBy = ascending ? $"{elementOrAttributePath}[asc]" : $"{elementOrAttributePath}[desc]" };
        return this;
    }

    public ILanguagesQuery Skip(int skip)
    {
        _params = _params with { Skip = skip };
        return this;
    }

    public ILanguagesQuery Limit(int limit)
    {
        _params = _params with { Limit = limit };
        return this;
    }

    public Task<IDeliveryLanguageListingResponse> ExecuteAsync()
    {
        return _api.GetLanguagesInternalAsync(_params, null);
    }
}


