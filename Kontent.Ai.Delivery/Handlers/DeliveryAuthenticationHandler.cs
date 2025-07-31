using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.Extensions;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Handlers;

/// <summary>
/// Delivery-specific authentication handler that extends Core's authentication handler
/// with support for Preview API and Secure Access API keys.
/// </summary>
public sealed class DeliveryAuthenticationHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<DeliveryOptions> _deliveryOptionsMonitor;
    private readonly string? _optionsName;

    /// <summary>
    /// Initializes a new instance of the DeliveryAuthenticationHandler with default options.
    /// </summary>
    /// <param name="deliveryOptionsMonitor">The delivery client options monitor for retrieving authentication configuration.</param>
    public DeliveryAuthenticationHandler(IOptionsMonitor<DeliveryOptions> deliveryOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(deliveryOptionsMonitor);
        _deliveryOptionsMonitor = deliveryOptionsMonitor;
    }

    /// <summary>
    /// Initializes a new instance of the DeliveryAuthenticationHandler with named options.
    /// </summary>
    /// <param name="deliveryOptionsMonitor">The delivery client options monitor for retrieving authentication configuration.</param>
    /// <param name="optionsName">The name of the options configuration to use.</param>
    public DeliveryAuthenticationHandler(IOptionsMonitor<DeliveryOptions> deliveryOptionsMonitor, string optionsName)
    {
        ArgumentNullException.ThrowIfNull(deliveryOptionsMonitor);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsName);

        _deliveryOptionsMonitor = deliveryOptionsMonitor;
        _optionsName = optionsName;
    }

    /// <summary>
    /// Sends an HTTP request with delivery-specific authentication headers added.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the delivery client options (named or default)
        var deliveryOptions = string.IsNullOrEmpty(_optionsName)
            ? _deliveryOptionsMonitor.CurrentValue
            : _deliveryOptionsMonitor.Get(_optionsName);

        // Add appropriate authorization header based on delivery options
        var apiKey = deliveryOptions?.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.AddAuthorizationHeader("Bearer", apiKey);
        }

        // Continue with the request pipeline
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}