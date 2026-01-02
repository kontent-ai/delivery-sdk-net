using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Handlers;

/// <summary>
/// DelegatingHandler that injects SDK and source tracking headers on every request.
/// This handler automatically adds X-KC-SDKID and X-KC-SOURCE headers according to Kontent.ai guidelines.
/// </summary>
public sealed class TrackingHandler : DelegatingHandler
{
    private readonly ILogger<TrackingHandler>? _logger;

    /// <summary>
    /// Initializes a new instance of the TrackingHandler.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public TrackingHandler(ILogger<TrackingHandler>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTTP request with tracking headers added.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add tracking headers to the request
        request.Headers.AddSdkTrackingHeader();
        request.Headers.AddSourceTrackingHeader();

        // Log tracking headers added (at Trace level since this happens on every request)
        if (_logger != null)
            LoggerMessages.HttpTrackingHeadersAdded(_logger, HttpRequestHeadersExtensions.GetSdkVersion());

        // Continue with the request pipeline
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}