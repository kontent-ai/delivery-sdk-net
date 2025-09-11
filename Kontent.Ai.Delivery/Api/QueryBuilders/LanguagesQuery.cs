using System.Threading;

namespace Kontent.Ai.Delivery.Api.QueryBuilders;

/// <inheritdoc cref="ILanguagesQuery"/>
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

    public async Task<IDeliveryResult<IReadOnlyList<ILanguage>>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool? wait = _waitForLoadingNewContentOverride ?? _getDefaultWaitForNewContent();
        var response = await _api.GetLanguagesInternalAsync(_params, wait).ConfigureAwait(false);
        var deliveryResult = await response.ToDeliveryResultAsync().ConfigureAwait(false);

        return deliveryResult.Map(response => response.Languages.AsReadOnly());
    }
}


