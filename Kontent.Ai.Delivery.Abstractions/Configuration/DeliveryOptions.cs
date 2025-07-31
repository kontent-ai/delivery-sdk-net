using System;

namespace Kontent.Ai.Delivery.Abstractions
{
    /// <summary>
    /// Represents configuration of the <see cref="IDeliveryClient"/>.
    /// </summary>
    public record DeliveryOptions
    {
        /// <summary>
        /// Gets or sets the environment ID.
        /// </summary>
        public string EnvironmentId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets a value that determines if the client uses resilience policies.
        /// </summary>
        public bool EnableResilience { get; init; } = true;

        /// <summary>
        /// Gets or sets the format of the Production API endpoint address.
        /// </summary>
        public string ProductionEndpoint { get; init; } = "https://deliver.kontent.ai/";

        /// <summary>
        /// Gets or sets the format of the Preview API endpoint address.
        /// </summary>
        public string PreviewEndpoint { get; init; } = "https://preview-deliver.kontent.ai/";

        /// <summary>
        /// Gets or sets the API key that is used to retrieve content with the Preview API.
        /// </summary>
        public string? PreviewApiKey { get; init; }

        /// <summary>
        /// Gets or sets a value that determines if the Preview API is used to retrieve content.
        /// If the Preview API is used the <see cref="PreviewApiKey"/> must be set.
        /// </summary>
        public bool UsePreviewApi { get; init; } = false;

        /// <summary>
        /// Gets or sets a value that determines if the client provides content that is always up-to-date.
        /// We recommend to wait for new content when you have received a webhook notification.
        /// However, the request might take longer than usual to complete.
        /// </summary>
        public bool WaitForLoadingNewContent { get; init; }

        /// <summary>
        /// Gets or sets a value that determines if the client sends the secure access API key to retrieve content with the Production API.
        /// This key is required to retrieve content when secure access is enabled.
        /// To retrieve content when secure access is enabled the <see cref="SecureAccessApiKey"/> must be set.
        /// </summary>
        public bool UseSecureAccess { get; init; } = false;

        /// <summary>
        /// Gets or sets the API key that is used to retrieve content with the Production API when secure access is enabled.
        /// </summary>
        public string? SecureAccessApiKey { get; init; }

        /// <summary>
        /// Gets or sets configuration of the default retry policy.
        /// </summary>
        public DefaultRetryPolicyOptions DefaultRetryPolicyOptions { get; init; } = new DefaultRetryPolicyOptions();

        /// <summary>
        /// Gets or sets a value that determines if the client includes the total number of items matching the search criteria in response.
        /// This behavior can also be enabled for individual requests with the IncludeTotalCountParameter.
        /// </summary>
        public bool IncludeTotalCount { get; init; }

        /// <summary>
        /// Gets or sets a value of codename for the rendition preset to be applied by default to the base asset URL path.
        /// If no value is specified, asset URLs will always point to non-customized variant of the image.
        /// </summary>
        public string? DefaultRenditionPreset { get; init; }

        /// <summary>
        /// The name of the service configuration this options object is related to.
        /// </summary>
        [Obsolete("#312")]
        internal string? Name { get; init; }
    }
}
