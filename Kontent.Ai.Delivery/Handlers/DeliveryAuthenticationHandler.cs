using System.Net.Http.Headers;
using Kontent.Ai.Delivery.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kontent.Ai.Delivery.Handlers;

/// <summary>
/// DelegatingHandler that injects authentication header and environment ID into a request.
/// </summary>
internal sealed class DeliveryAuthenticationHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<DeliveryOptions> _monitor;
    private readonly string? _name;
    private readonly ILogger<DeliveryAuthenticationHandler>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="monitor">Instance of <see cref="IOptionsMonitor{DeliveryOptions}"/>.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeliveryAuthenticationHandler(
        IOptionsMonitor<DeliveryOptions> monitor,
        ILogger<DeliveryAuthenticationHandler>? logger = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryAuthenticationHandler"/> class with named options.
    /// </summary>
    /// <param name="monitor">Instance of <see cref="IOptionsMonitor{DeliveryOptions}"/>.</param>
    /// <param name="optionsName">The name of the options.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DeliveryAuthenticationHandler(
        IOptionsMonitor<DeliveryOptions> monitor,
        string optionsName,
        ILogger<DeliveryAuthenticationHandler>? logger = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        ArgumentException.ThrowIfNullOrWhiteSpace(optionsName);
        _name = optionsName;
        _logger = logger;
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var opts = _name is null ? _monitor.CurrentValue : _monitor.Get(_name);
        var baseUri = new Uri(opts.GetBaseUrl().TrimEnd('/'), UriKind.Absolute);
        var isTrustedDeliveryRequest = request.RequestUri is null ||
                                       !request.RequestUri.IsAbsoluteUri ||
                                       ShouldRewriteUri(request.RequestUri, baseUri);

        if (!isTrustedDeliveryRequest)
        {
            // Never propagate SDK auth headers to external targets.
            request.Headers.Authorization = null;
            if (_logger is not null)
                LoggerMessages.HttpAuthCleared(_logger);

            return base.SendAsync(request, cancellationToken);
        }

        // 1) Base endpoint rewrite (runtime switchable prod/preview)

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
            var originalHost = request.RequestUri.Host;
            var ub = new UriBuilder(request.RequestUri)
            {
                Scheme = baseUri.Scheme,
                Host = baseUri.Host,
                Port = baseUri.IsDefaultPort ? -1 : baseUri.Port
            };
            request.RequestUri = ub.Uri;

            // Log endpoint rewriting if host changed
            if (_logger is not null && !originalHost.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
                LoggerMessages.HttpEndpointRewritten(_logger, originalHost, baseUri.Host);
        }

        // 2) Auth (Bearer key) - assign after target classification to avoid leaking SDK keys.
        var apiKey = opts.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Log auth type (NEVER log the actual API key)
            if (_logger is not null)
            {
                var authType = GetAuthType(opts);
                LoggerMessages.HttpAuthSet(_logger, authType, opts.EnvironmentId ?? "unknown");
            }
        }
        else
        {
            request.Headers.Authorization = null;
            if (_logger is not null)
                LoggerMessages.HttpAuthCleared(_logger);
        }

        // 3) Inject "/{environmentId}" as first path segment if missing.
        var env = opts.EnvironmentId?.Trim('/');
        if (!string.IsNullOrWhiteSpace(env) && request.RequestUri is not null)
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

                // Log environment ID injection
                if (_logger is not null)
                    LoggerMessages.HttpEnvironmentIdInjected(_logger, env);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static string GetAuthType(DeliveryOptions opts)
        => (opts.UsePreviewApi, opts.UseSecureAccess) switch
        {
            (true, _) => "Preview",
            (_, true) => "SecureAccess",
            _ => "Production"
        };
}
