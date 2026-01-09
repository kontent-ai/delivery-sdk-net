namespace Kontent.Ai.Delivery.Configuration;

/// <summary>
/// A builder of <see cref="DeliveryOptions"/> instances.
/// </summary>
public class DeliveryOptionsBuilder : IDeliveryOptionsBuilder // TODO: add injection of type and model providers etc.
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
    /// Use a custom endpoint for the Production or Preview API.
    /// </summary>
    /// <param name="endpoint">A custom endpoint URL.</param>
    public IDeliveryOptionsBuilder WithCustomEndpoint(string endpoint)
    {
        SetCustomEndpoint(endpoint);
        return this;
    }

    /// <summary>
    /// Use a custom endpoint for the Production or Preview API.
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
    /// Enable waiting for loading new content globally via DeliveryOptions.
    /// </summary>
    public IDeliveryOptionsBuilder WaitForLoadingNewContent()
    {
        _options.WaitForLoadingNewContent = true;
        return this;
    }

    private void SetCustomEndpoint(string endpoint)
    {
        if (_options.UsePreviewApi)
        {
            _options.PreviewEndpoint = endpoint;
        }
        else
        {
            _options.ProductionEndpoint = endpoint;
        }
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
        IncludeTotalCount = _options.IncludeTotalCount,
        WaitForLoadingNewContent = _options.WaitForLoadingNewContent,
        DefaultRenditionPreset = _options.DefaultRenditionPreset
    };
}