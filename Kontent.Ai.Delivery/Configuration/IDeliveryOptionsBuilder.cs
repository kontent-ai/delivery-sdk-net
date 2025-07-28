using System;
using Kontent.Ai.Delivery.Abstractions;

namespace Kontent.Ai.Delivery.Configuration
{
    /// <summary>
    /// A builder abstraction for creating <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public interface IDeliveryOptionsBuilder
    {
        /// <summary>
        /// Configure for Production API.
        /// </summary>
        IDeliveryOptionsBuilder UseProduction();

        /// <summary>
        /// Configure for Production API with secure access.
        /// </summary>
        /// <param name="secureAccessApiKey">An API key for secure access.</param>
        IDeliveryOptionsBuilder UseProduction(string secureAccessApiKey);

        /// <summary>
        /// Configure for Preview API.
        /// </summary>
        /// <param name="previewApiKey">A Preview API key.</param>
        IDeliveryOptionsBuilder UsePreview(string previewApiKey);

        /// <summary>
        /// Disable retry policy for HTTP requests.
        /// </summary>
        IDeliveryOptionsBuilder DisableRetryPolicy();

        /// <summary>
        /// Use a custom endpoint for the Production or Preview API.
        /// </summary>
        /// <param name="endpoint">A custom endpoint URL.</param>
        IDeliveryOptionsBuilder WithCustomEndpoint(string endpoint);

        /// <summary>
        /// Use a custom endpoint for the Production or Preview API.
        /// </summary>
        /// <param name="endpoint">A custom endpoint URI.</param>
        IDeliveryOptionsBuilder WithCustomEndpoint(Uri endpoint);

        /// <summary>
        /// Apply rendition of given preset to the asset URLs by default.
        /// </summary>
        /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
        IDeliveryOptionsBuilder WithDefaultRenditionPreset(string presetCodename);

        /// <summary>
        /// Configure custom retry policy options.
        /// </summary>
        /// <param name="retryPolicyOptions">Configuration of the retry policy.</param>
        IDeliveryOptionsBuilder WithRetryPolicyOptions(DefaultRetryPolicyOptions retryPolicyOptions);

        /// <summary>
        /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        DeliveryOptions Build();
    }

    #region Legacy interfaces - kept for backward compatibility

    /// <summary>
    /// Legacy builder abstraction of mandatory setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    [Obsolete("Use simplified DeliveryOptionsBuilder.Create(environmentId) API")]
    public interface IDeliveryApiConfiguration
    {
        /// <summary>
        /// Use Production API with secure access disabled to retrieve content.
        /// </summary>
        IOptionalDeliveryConfiguration UseProductionApi();

        /// <summary>
        /// Use Production API with secure access enabled to retrieve content.
        /// </summary>
        /// <param name="secureAccessApiKey">An API key for secure access.</param>
        IOptionalDeliveryConfiguration UseProductionApi(string secureAccessApiKey);

        /// <summary>
        /// Use Preview API to retrieve content.
        /// </summary>
        /// <param name="previewApiKey">A Preview API key.</param>
        IOptionalDeliveryConfiguration UsePreviewApi(string previewApiKey);
    }

    /// <summary>
    /// Legacy builder abstraction of optional setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    [Obsolete("Use simplified DeliveryOptionsBuilder.Create(environmentId) API")]
    public interface IOptionalDeliveryConfiguration : IDeliveryOptionsBuild
    {
        /// <summary>
        /// Disable retry policy for HTTP requests.
        /// </summary>
        IOptionalDeliveryConfiguration DisableRetryPolicy();

        /// <summary>
        /// Provide content that is always up-to-date.
        /// We recommend to wait for new content when you have received a webhook notification.
        /// However, the request might take longer than usual to complete.
        /// </summary>
        IOptionalDeliveryConfiguration WaitForLoadingNewContent();

        /// <summary>
        /// Include the total number of items matching the search criteria in the response.
        /// This behavior can also be enabled for individual requests with query parameters.
        /// Please note that using this option might increase the response time.
        /// </summary>
        IOptionalDeliveryConfiguration IncludeTotalCount();
        
        /// <summary>
        /// Change configuration of the default retry policy.
        /// </summary>
        /// <param name="retryPolicyOptions">Configuration of the default retry policy.</param>
        IOptionalDeliveryConfiguration WithDefaultRetryPolicyOptions(DefaultRetryPolicyOptions retryPolicyOptions);

        /// <summary>
        /// Use a custom format for the Production or Preview API endpoint address.
        /// </summary>
        /// <param name="customEndpoint">A custom format for the Production API endpoint address.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(string customEndpoint);

        /// <summary>
        /// Use a custom format for the Production or Preview API endpoint address.
        /// </summary>
        /// <param name="customEndpoint">A custom endpoint URI.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(Uri customEndpoint);

        /// <summary>
        /// Apply rendition of given preset to the asset URLs by default.
        /// </summary>
        /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
        IOptionalDeliveryConfiguration WithDefaultRenditionPreset(string presetCodename);
    }

    /// <summary>
    /// Legacy builder abstraction of the last step in setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    [Obsolete("Use simplified DeliveryOptionsBuilder.Create(environmentId) API")]
    public interface IDeliveryOptionsBuild
    {
        /// <summary>
        /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        DeliveryOptions Build();
    }

    /// <summary>
    /// Legacy builder interface with environment ID setup.
    /// </summary>
    [Obsolete("Use simplified DeliveryOptionsBuilder.Create(environmentId) API")]
    public interface ILegacyDeliveryOptionsBuilder
    {
        /// <summary>
        /// Use environment identifier.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        IDeliveryApiConfiguration WithEnvironmentId(string environmentId);

        /// <summary>
        /// Use environment identifier.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        IDeliveryApiConfiguration WithEnvironmentId(Guid environmentId);
    }

    #endregion
}
