namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// A builder of <see cref="DeliveryOptions"/> instances.
/// </summary>
public sealed class DeliveryOptionsBuilder : IDeliveryOptionsBuilder
{
    private readonly DeliveryOptions _options = new();
    private DeliveryOptionsBuilder() { }

    /// <summary>
    /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class.
    /// </summary>
    public static IDeliveryOptionsBuilder CreateInstance() => new DeliveryOptionsBuilder();

    /// <summary>
    /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class with the specified environment ID.
    /// </summary>
    /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
    public IDeliveryOptionsBuilder WithEnvironmentId(string environmentId)
    {
        _options.EnvironmentId = environmentId;
        return this;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class with the specified environment ID.
    /// </summary>
    /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
    public IDeliveryOptionsBuilder WithEnvironmentId(Guid environmentId)
    {
        _options.EnvironmentId = environmentId.ToString();
        return this;
    }

    /// <summary>
    /// Configure for Production API.
    /// </summary>
    public IDeliveryOptionsBuilder UseProductionApi()
    {
        _options.UsePreviewApi = false;
        _options.UseSecureAccess = false;
        return this;
    }

    /// <summary>
    /// Configure for Production API with secure access.
    /// </summary>
    /// <param name="secureAccessApiKey">An API key for secure access.</param>
    public IDeliveryOptionsBuilder UseProductionApi(string secureAccessApiKey)
    {
        _options.UsePreviewApi = false;
        _options.UseSecureAccess = true;
        _options.SecureAccessApiKey = secureAccessApiKey;
        return this;
    }

    /// <summary>
    /// Configure for Preview API.
    /// </summary>
    /// <param name="previewApiKey">A Preview API key.</param>
    public IDeliveryOptionsBuilder UsePreviewApi(string previewApiKey)
    {
        _options.UsePreviewApi = true;
        _options.PreviewApiKey = previewApiKey;
        _options.UseSecureAccess = false;
        return this;
    }

    /// <summary>
    /// Disable retry policy for HTTP requests.
    /// </summary>
    public IDeliveryOptionsBuilder DisableRetryPolicy()
    {
        _options.EnableResilience = false;
        return this;
    }

    /// <summary>
    /// Use a custom endpoint for both the Production and Preview APIs.
    /// </summary>
    /// <param name="endpoint">A custom endpoint URL.</param>
    public IDeliveryOptionsBuilder WithCustomEndpoint(string endpoint)
    {
        SetCustomEndpoint(endpoint);
        return this;
    }

    /// <summary>
    /// Use a custom endpoint for both the Production and Preview APIs.
    /// </summary>
    /// <param name="endpoint">A custom endpoint URI.</param>
    public IDeliveryOptionsBuilder WithCustomEndpoint(Uri endpoint)
    {
        SetCustomEndpoint(endpoint.AbsoluteUri);
        return this;
    }

    /// <summary>
    /// Apply rendition of given preset to the asset URLs by default.
    /// </summary>
    /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
    public IDeliveryOptionsBuilder WithDefaultRenditionPreset(string presetCodename)
    {
        _options.DefaultRenditionPreset = presetCodename;
        return this;
    }

    /// <summary>
    /// Use a custom domain for asset URLs.
    /// </summary>
    /// <param name="customDomain">A custom asset domain URL (e.g. "https://assets.example.com").</param>
    /// <exception cref="ArgumentException">Thrown when the domain contains a non-root path, query string, or fragment.</exception>
    public IDeliveryOptionsBuilder WithCustomAssetDomain(string customDomain)
    {
        if (!string.IsNullOrWhiteSpace(customDomain) &&
            Uri.TryCreate(customDomain, UriKind.Absolute, out var parsed))
        {
            ValidateCustomAssetDomainUri(parsed);
        }

        _options.CustomAssetDomain = customDomain;
        return this;
    }

    /// <summary>
    /// Use a custom domain for asset URLs.
    /// </summary>
    /// <param name="customDomain">A custom asset domain URI.</param>
    /// <exception cref="ArgumentException">Thrown when the domain contains a non-root path, query string, or fragment.</exception>
    public IDeliveryOptionsBuilder WithCustomAssetDomain(Uri customDomain)
    {
        ArgumentNullException.ThrowIfNull(customDomain);
        ValidateCustomAssetDomainUri(customDomain);
        _options.CustomAssetDomain = customDomain.AbsoluteUri;
        return this;
    }

    private static void ValidateCustomAssetDomainUri(Uri uri)
    {
        if (uri.AbsolutePath is not ("/" or ""))
        {
            throw new ArgumentException(
                $"CustomAssetDomain must be a root domain without a path (e.g. 'https://assets.example.com'). " +
                $"Got: '{uri.AbsoluteUri}'. The path '{uri.AbsolutePath}' would be silently ignored.",
                nameof(uri));
        }

        if (!string.IsNullOrEmpty(uri.Query))
        {
            throw new ArgumentException(
                $"CustomAssetDomain must not contain a query string. Got: '{uri.AbsoluteUri}'.",
                nameof(uri));
        }

        if (!string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException(
                $"CustomAssetDomain must not contain a fragment. Got: '{uri.AbsoluteUri}'.",
                nameof(uri));
        }
    }

    private void SetCustomEndpoint(string endpoint)
    {
        // Apply to both endpoints so behavior is deterministic regardless of call order
        // (e.g. WithCustomEndpoint() called before/after UsePreviewApi()).
        _options.PreviewEndpoint = endpoint;
        _options.ProductionEndpoint = endpoint;
    }

    /// <summary>
    /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
    /// </summary>
    public DeliveryOptions Build() => new()
    {
        EnvironmentId = _options.EnvironmentId,
        EnableResilience = _options.EnableResilience,
        ProductionEndpoint = _options.ProductionEndpoint,
        PreviewEndpoint = _options.PreviewEndpoint,
        PreviewApiKey = _options.PreviewApiKey,
        UsePreviewApi = _options.UsePreviewApi,
        UseSecureAccess = _options.UseSecureAccess,
        SecureAccessApiKey = _options.SecureAccessApiKey,
        DefaultRenditionPreset = _options.DefaultRenditionPreset,
        CustomAssetDomain = _options.CustomAssetDomain
    };
}
