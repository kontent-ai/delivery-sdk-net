using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using System.Threading;

namespace Kontent.Ai.Delivery.Handlers;

/// <summary>
/// DelegatingHandler that injects authentication header and environment ID into a request.
/// </summary>
public sealed class DeliveryAuthenticationHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<DeliveryOptions> _monitor;
    private readonly string? _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="monitor">Instance of <see cref="IOptionsMonitor{DeliveryOptions}"/>.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeliveryAuthenticationHandler(IOptionsMonitor<DeliveryOptions> monitor) =>
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAuthenticationHandler"/> class with named options.
    /// </summary>
    /// <param name="monitor">Instance of <see cref="IOptionsMonitor{DeliveryOptions}"/>.</param>
    /// <param name="optionsName">The name of the options.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeliveryAuthenticationHandler(IOptionsMonitor<DeliveryOptions> monitor, string optionsName)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsName);
        _name = optionsName;
    }

    /// <summary>
    /// Determines whether an absolute URI should be rewritten to use the configured base URL.
    /// Only Kontent.ai Delivery API URLs should be rewritten (for runtime endpoint switching).
    /// External URLs (CDN, webhooks, management API, etc.) should be left untouched.
    /// </summary>
    /// <param name="requestUri">The absolute request URI.</param>
    /// <param name="configuredBase">The configured base URL from options.</param>
    /// <returns>True if the URI should be rewritten, false otherwise.</returns>
    private static bool ShouldRewriteUri(Uri requestUri, Uri configuredBase)
    {
        var host = requestUri.Host;

        // Match configured base host (supports custom domains)
        if (host.Equals(configuredBase.Host, StringComparison.OrdinalIgnoreCase))
            return true;

        // Match standard Kontent.ai delivery endpoints only
        // - deliver.kontent.ai (production delivery)
        // - preview-deliver.kontent.ai (preview delivery)
        // Explicitly exclude management API, webhooks, etc.
        if (host.Equals("deliver.kontent.ai", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("preview-deliver.kontent.ai", StringComparison.OrdinalIgnoreCase))
            return true;

        // Don't rewrite external URLs (CDN, webhooks, management API, etc.)
        return false;
    }

    /// <summary>
    /// Sends an HTTP request with authentication header and environment ID injected.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var opts = _name is null ? _monitor.CurrentValue : _monitor.Get(_name);

        // 1) Auth (Bearer key) - always assign to ensure stale headers are cleared
        var apiKey = opts.GetApiKey();
        request.Headers.Authorization = !string.IsNullOrWhiteSpace(apiKey)
            ? new AuthenticationHeaderValue("Bearer", apiKey)
            : null;

        // 2) Base endpoint rewrite (runtime switchable prod/preview)
        var baseUri = new Uri(opts.GetBaseUrl().TrimEnd('/'), UriKind.Absolute);

        if (request.RequestUri is null)
        {
            request.RequestUri = baseUri;
        }
        else if (!request.RequestUri.IsAbsoluteUri)
        {
            // Relative URI - combine with base
            request.RequestUri = new Uri(baseUri, request.RequestUri);
        }
        else if (ShouldRewriteUri(request.RequestUri, baseUri))
        {
            // Absolute URI targeting Kontent.ai API - rewrite to support runtime endpoint switching
            // This allows switching between preview/production at runtime even though BaseAddress is static
            var ub = new UriBuilder(request.RequestUri)
            {
                Scheme = baseUri.Scheme,
                Host = baseUri.Host,
                Port = baseUri.IsDefaultPort ? -1 : baseUri.Port
            };
            request.RequestUri = ub.Uri;
        }
        // else: External absolute URI (CDN, webhooks, etc.) - leave untouched

        // 3) Inject "/{environmentId}" as first path segment if missing
        //    Only do this for Kontent.ai API URLs, not external URLs (CDN, webhooks, etc.)
        var env = opts.EnvironmentId?.Trim('/');
        if (!string.IsNullOrWhiteSpace(env) &&
            request.RequestUri != null &&
            ShouldRewriteUri(request.RequestUri, baseUri))
        {
            var uri = request.RequestUri;
            var path = uri.AbsolutePath;                 // always starts with "/"
            var envPrefix = "/" + env;                   // "/{env}"

            // If path is exactly "/{env}" or starts with "/{env}/", do nothing; else prefix it.
            var hasEnv =
                path.Equals(envPrefix, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(envPrefix + "/", StringComparison.OrdinalIgnoreCase);

            if (!hasEnv)
            {
                var ub = new UriBuilder(uri) { Path = envPrefix + path }; // "/env" + "/items/..." => "/env/items/..."
                request.RequestUri = ub.Uri;                               // keeps query/fragment/host intact
            }
        }

        return base.SendAsync(request, ct);
    }
}