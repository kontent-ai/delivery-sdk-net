﻿using System;
using Kentico.Kontent.Delivery.RetryPolicy;

namespace Kentico.Kontent.Delivery.Builders.DeliveryOptions
{
    /// <summary>
    /// Defines the contracts of the mandatory steps for building a <see cref="DeliveryOptions"/> instance.
    /// </summary>
    public interface IDeliveryOptionsBuilder
    {
        /// <summary>
        /// A mandatory step of the <see cref="DeliveryOptionsBuilder"/> for specifying Kentico Kontent project id.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Kontent project.</param>
        IDeliveryApiConfiguration WithProjectId(string projectId);

        /// <summary>
        /// A mandatory step of the <see cref="DeliveryOptionsBuilder"/> for specifying Kentico Kontent project id.
        /// </summary>
        /// <param name="projectId">The identifier of the Kentico Kontent project.</param>
        IDeliveryApiConfiguration WithProjectId(Guid projectId);
    }

    /// <summary>
    /// Defines the contracts of different APIs that might be used.
    /// </summary>
    public interface IDeliveryApiConfiguration
    {
        /// <summary>
        /// Sets the Delivery Client to make requests to a Production API.
        /// </summary>
        IOptionalDeliveryConfiguration UseProductionApi { get; }

        /// <summary>
        /// Sets the Delivery Client to make requests to a Preview API.
        /// </summary>
        /// <param name="previewApiKey">A Preview API key</param>
        IOptionalDeliveryConfiguration UsePreviewApi(string previewApiKey);

        /// <summary>
        /// Sets the Delivery Client to make requests to a Secured Production API.
        /// </summary>
        /// <param name="securedProductionApiKey">An API key for secure access.</param>
        IOptionalDeliveryConfiguration UseSecuredProductionApi(string securedProductionApiKey);
    }

    /// <summary>
    /// Defines the contracts of the optional steps for building a <see cref="DeliveryOptions"/> instance.
    /// </summary>
    public interface IOptionalDeliveryConfiguration : IDeliveryOptionsBuild
    {
        /// <summary>
        /// An optional step that disables retry policy for HTTP requests.
        /// </summary>
        IOptionalDeliveryConfiguration DisableRetryLogic { get; }

        /// <summary>
        /// An optional step that sets the client to wait for updated content.
        /// It should be used when you are acting upon a webhook call.
        /// </summary>
        IOptionalDeliveryConfiguration WaitForLoadingNewContent { get; }

        /// <summary>
        /// An optional step that sets the custom options for retry policy.
        /// </summary>
        /// <remarks>
        /// The retry policy options in <see cref="RetryPolicyOptions"/> are used only in the default implementation of the <see cref="IRetryPolicyProvider" /> interface.
        /// Setting the value to null will result in disabled retry policy.
        /// If this method does not specify otherwise, the default options will be used.
        /// </remarks>
        /// <param name="retryPolicyOptions">Custom retry policy options.</param>
        IOptionalDeliveryConfiguration WithRetryPolicyOptions(RetryPolicyOptions retryPolicyOptions);

        /// <summary>
        /// An optional step that sets a custom endpoint for a chosen API. If "{0}" is provided in the URL, it gets replaced by the projectId.
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom endpoint URL address.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(string customEndpoint);

        /// <summary>
        /// An optional step that sets a custom endpoint for a chosen API.
        /// </summary>
        /// <remarks>
        /// While both HTTP and HTTPS protocols are supported, we recommend always using HTTPS.
        /// </remarks>
        /// <param name="customEndpoint">A custom endpoint URI.</param>
        IOptionalDeliveryConfiguration WithCustomEndpoint(Uri customEndpoint);
    }
    
    /// <summary>
    /// Defines the contract of the last build step that creates a new instance of the of the <see cref="DeliveryOptions"/> class.
    /// </summary>
    public interface IDeliveryOptionsBuild
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptions" /> class that configures the Kentico Delivery Client.
        /// </summary>
        /// <returns>A new <see cref="DeliveryOptions"/> instance</returns>
        Delivery.DeliveryOptions Build();
    }
}