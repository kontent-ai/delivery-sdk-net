using System.Runtime.CompilerServices;
using System.Threading;
using Kontent.Ai.Delivery.ContentItems;

/// <inheritdoc cref="IDeliveryItemsFeed{TModel}"/>
internal sealed class DeliveryItemsFeed<TModel>(DeliveryItemsFeed<TModel>.GetPageAsync getPage, string? startToken = null) : IDeliveryItemsFeed<TModel>
    where TModel : IElementsModel
{
    // Delegate that calls Refit and returns headers+body
    internal delegate Task<IApiResponse<DeliveryItemsFeedResponse<TModel>>> GetPageAsync(
        string? continuationToken, CancellationToken ct);

    private readonly GetPageAsync _getPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
    private string? _continuation = startToken;
    private bool _exhausted;
    public bool HasMoreResults => !_exhausted;

    public async Task<IDeliveryItemsFeedResponse<TModel>> FetchNextBatchAsync(
        string? continuationToken = null, CancellationToken ct = default)
    {
        if (_exhausted)
            throw new InvalidOperationException("The feed has been fully enumerated.");

        var token = continuationToken ?? _continuation;
        var resp = await _getPage(token, ct).ConfigureAwait(false);

        // Map network errors to empty page (or throw—your call)
        if (!resp.IsSuccessStatusCode || resp.Content is null)
            return new DeliveryItemsFeedResponse<TModel> { Items = [] };

        // Update continuation from headers
        _continuation = resp.Continuation();
        _exhausted = string.IsNullOrEmpty(_continuation);

        return resp.Content; // contains Items only
    }

    // Removed AsAsyncEnumerable to keep a single, simple consumption pattern via FetchNextBatchAsync
}
