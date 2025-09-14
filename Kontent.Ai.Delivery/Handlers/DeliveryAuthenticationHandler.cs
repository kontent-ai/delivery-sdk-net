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
    /// Sends an HTTP request with authentication header and environment ID injected.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var opts = _name is null ? _monitor.CurrentValue : _monitor.Get(_name);

        // 1) Auth (Bearer key)
        var apiKey = opts.GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // 2) Base endpoint rewrite (runtime switchable prod/preview)
        var baseUri = new Uri(opts.GetBaseUrl().TrimEnd('/'), UriKind.Absolute);

        if (request.RequestUri is null)
        {
            request.RequestUri = baseUri;
        }
        else if (!request.RequestUri.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(baseUri, request.RequestUri);
        }
        else
        {
            // Absolute -> replace scheme/host/port from current base (keep path/query/fragment)
            var ub = new UriBuilder(request.RequestUri)
            {
                Scheme = baseUri.Scheme,
                Host   = baseUri.Host,
                Port   = baseUri.IsDefaultPort ? -1 : baseUri.Port
            };
            request.RequestUri = ub.Uri;
        }

        // 3) Inject "/{environmentId}" as first path segment if missing
        var env = opts.EnvironmentId?.Trim('/');
        if (!string.IsNullOrWhiteSpace(env))
        {
            var uri = request.RequestUri!;
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
