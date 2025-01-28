using System;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Delivery.Abstractions;


namespace Kontent.Ai.Delivery.Configuration
{
    /// <summary>
    /// A builder abstraction of mandatory setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public interface IDeliveryOptionsBuilder
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

    /// <summary>
    /// A builder abstraction of API setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
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
    /// A builder abstraction of optional setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
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
        /// This behavior can also be enabled for individual requests with the <see cref="IncludeTotalCountParameter"/>.
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
        /// The environment identifier will be inserted at the position of the first format item "{0}".
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom format for the Production API endpoint address.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(string customEndpoint);

        /// <summary>
        /// Use a custom format for the Production or Preview API endpoint address.
        /// The environment identifier will be inserted at the position of the first format item "{0}".
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom endpoint URI.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(Uri customEndpoint);

        /// <summary>
        /// Apply rendition of given preset to the asset URLs by default.
        /// </summary>
        /// <remarks> 
        /// If not configured, asset URLs will always point to non-customized variant of the image.
        /// </remarks>
        /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
        IOptionalDeliveryConfiguration WithDefaultRenditionPreset(string presetCodename);

        /// <summary>
        /// Replaces the base URL of the asset URLs in the API response. The asset URL will retain all the identifying information such as project id, asset id and file name.<br/>
        /// <b>NOTE: </b>Do not specify a trailing backslash in the URL.<br/>
        /// <b>Example where url is defined as "https://www.example.com/assets":</b><br/>
        /// <b>Original Value:</b> https://preview-assets-us-01.kc-usercontent.com/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf <br/>
        /// <b>New Value:</b> https://www.example.com/assets/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf
        /// </summary>
        /// <param name="url">URL to substitute for asset urls</param>
        /// <returns></returns>
        IOptionalDeliveryConfiguration WithAssetUrlReplacement(string url);

        /// <summary>
        /// Replaces the base URL of the asset URLs in the API response. The asset URL will retain all the identifying information such as project id, asset id and file name.<br/>
        /// <b>NOTE: </b>Do not specify a trailing backslash in the URL.<br/>
        /// <b>Example where url is defined as "https://www.example.com/assets":</b><br/>
        /// <b>Original Value:</b> https://preview-assets-us-01.kc-usercontent.com/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf <br/>
        /// <b>New Value:</b> https://www.example.com/assets/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf
        /// </summary>
        /// <param name="url">URL to substitute for asset urls</param>
        /// <returns></returns>
        IOptionalDeliveryConfiguration WithAssetUrlReplacement(Uri url);
    }

    /// <summary>
    /// A builder abstraction of the last step in setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public interface IDeliveryOptionsBuild
    {
        /// <summary>
        /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        DeliveryOptions Build();
    }
}
