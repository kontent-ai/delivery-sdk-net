using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;

namespace Kontent.Ai.Delivery.Handlers;

/// <summary>
/// DelegatingHandler that injects SDK and source tracking headers on every request.
/// This handler automatically adds X-KC-SDKID and X-KC-SOURCE headers according to Kontent.ai guidelines.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TrackingHandler.
/// </remarks>
/// <param name="logger">Optional logger instance.</param>
internal sealed class TrackingHandler(ILogger<TrackingHandler>? logger = null) : DelegatingHandler
{

    /// <summary>
    /// Sends an HTTP request with tracking headers added.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add tracking headers to the request
        request.Headers.AddSdkTrackingHeader();
        request.Headers.AddSourceTrackingHeader();

        // Log tracking headers added (at Trace level since this happens on every request)
        if (logger is not null)
            LoggerMessages.HttpTrackingHeadersAdded(logger, HttpRequestHeadersExtensions.GetSdkVersion());

        // Continue with the request pipeline
        return base.SendAsync(request, cancellationToken);
    }
}
