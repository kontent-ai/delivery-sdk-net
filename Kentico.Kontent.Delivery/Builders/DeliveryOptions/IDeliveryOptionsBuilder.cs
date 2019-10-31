using System;

namespace Kentico.Kontent.Delivery.Builders.DeliveryOptions
{
    /// <summary>
    /// A builder abstraction of mandatory setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public interface IDeliveryOptionsBuilder
    {
        /// <summary>
        /// Use project identifier.
        /// </summary>
        /// <param name="projectId">The identifier of a Kentico Kontent project.</param>
        IDeliveryApiConfiguration WithProjectId(string projectId);

        /// <summary>
        /// Use project identifier.
        /// </summary>
        /// <param name="projectId">The identifier of a Kentico Kontent project.</param>
        IDeliveryApiConfiguration WithProjectId(Guid projectId);
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
        /// Provide information on how many items are there in total for a query to items endpoint.
        /// This can be used to determine total number of pages that can be retrieved.
        /// We don't recommend to enable this setting for all requests as this might increase response time.
        /// </summary>
        /// <returns></returns>
        IOptionalDeliveryConfiguration IncludeTotalCount();
        
        /// <summary>
        /// Change configuration of the default retry policy.
        /// </summary>
        /// <param name="retryPolicyOptions">Configuration of the default retry policy.</param>
        IOptionalDeliveryConfiguration WithDefaultRetryPolicyOptions(DefaultRetryPolicyOptions retryPolicyOptions);

        /// <summary>
        /// Use a custom format for the Production or Preview API endpoint address.
        /// The project identifier will be inserted at the position of the first format item "{0}".
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom format for the Production API endpoint address.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(string customEndpoint);

        /// <summary>
        /// Use a custom format for the Production or Preview API endpoint address.
        /// The project identifier will be inserted at the position of the first format item "{0}".
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom endpoint URI.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(Uri customEndpoint);
    }

    /// <summary>
    /// A builder abstraction of the last step in setup of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public interface IDeliveryOptionsBuild
    {
        /// <summary>
        /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        Delivery.DeliveryOptions Build();
    }
}
