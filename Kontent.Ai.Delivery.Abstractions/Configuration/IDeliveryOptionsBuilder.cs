namespace Kontent.Ai.Delivery.Abstractions;

/// <summary>
/// A builder abstraction for creating <see cref="DeliveryOptions"/> instances.
/// </summary>
public interface IDeliveryOptionsBuilder
{
    /// <summary>
    /// Configure for Production API.
    /// </summary>
    IDeliveryOptionsBuilder UseProductionApi();

    /// <summary>
    /// Configure for Production API with secure access.
    /// </summary>
    /// <param name="secureAccessApiKey">An API key for secure access.</param>
    IDeliveryOptionsBuilder UseProductionApi(string secureAccessApiKey);

    /// <summary>
    /// Configure for Preview API.
    /// </summary>
    /// <param name="previewApiKey">A Preview API key.</param>
    IDeliveryOptionsBuilder UsePreviewApi(string previewApiKey);

    /// <summary>
    /// Sets the environment ID for the delivery options.
    /// </summary>
    IDeliveryOptionsBuilder WithEnvironmentId(string environmentId);

    /// <summary>
    /// Sets the environment ID for the delivery options.
    /// </summary>
    IDeliveryOptionsBuilder WithEnvironmentId(Guid environmentId);

    /// <summary>
    /// Disable retry policy for HTTP requests.
    /// </summary>
    IDeliveryOptionsBuilder DisableRetryPolicy();

    /// <summary>
    /// Use a custom endpoint for both the Production and Preview APIs.
    /// </summary>
    /// <param name="endpoint">A custom endpoint URL.</param>
    IDeliveryOptionsBuilder WithCustomEndpoint(string endpoint);

    /// <summary>
    /// Use a custom endpoint for both the Production and Preview APIs.
    /// </summary>
    /// <param name="endpoint">A custom endpoint URI.</param>
    IDeliveryOptionsBuilder WithCustomEndpoint(Uri endpoint);

    /// <summary>
    /// Apply rendition of given preset to the asset URLs by default.
    /// </summary>
    /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
    IDeliveryOptionsBuilder WithDefaultRenditionPreset(string presetCodename);

    /// <summary>
    /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
    /// </summary>
    DeliveryOptions Build();
}
