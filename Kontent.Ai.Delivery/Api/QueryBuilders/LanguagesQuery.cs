using Kontent.Ai.Delivery.Abstractions.QueryBuilders;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

internal sealed class LanguagesQuery(IDeliveryApi api, Func<bool?> getDefaultWaitForNewContent) : ILanguagesQuery
{
    private readonly IDeliveryApi _api = api;
    private readonly Func<bool?> _getDefaultWaitForNewContent = getDefaultWaitForNewContent;
    private LanguagesParams _params = new();
    private bool? _waitForLoadingNewContentOverride;

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

    public ILanguagesQuery WaitForLoadingNewContent(bool enabled = true)
    {
        _waitForLoadingNewContentOverride = enabled;
        return this;
    }

    public Task<IDeliveryLanguageListingResponse> ExecuteAsync()
    {
        bool? header = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        return _api.GetLanguagesInternalAsync(_params, header);
    }
}


