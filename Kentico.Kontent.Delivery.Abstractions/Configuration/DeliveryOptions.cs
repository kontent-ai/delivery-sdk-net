namespace Kentico.Kontent.Delivery.Abstractions.Configuration
{
    /// <summary>
    /// Represents configuration of the <see cref="IDeliveryClient"/>.
    /// </summary>
    public class DeliveryOptions
    {
        /// <summary>
        /// Gets or sets the format of the Production API endpoint address.
        /// </summary>
        public string ProductionEndpoint { get; set; } = "https://deliver.kontent.ai/";

        /// <summary>
        /// Gets or sets the format of the Preview API endpoint address.
        /// </summary>
        public string PreviewEndpoint { get; set; } = "https://preview-deliver.kontent.ai/";

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the API key that is used to retrieve content with the Preview API.
        /// </summary>
        public string PreviewApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the Preview API is used to retrieve content.
        /// If the Preview API is used the <see cref="PreviewApiKey"/> must be set.
        /// </summary>
        public bool UsePreviewApi { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the client provides content that is always up-to-date.
        /// We recommend to wait for new content when you have received a webhook notification.
        /// However, the request might take longer than usual to complete.
        /// </summary>
        public bool WaitForLoadingNewContent { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the client sends the secure access API key to retrieve content with the Production API.
        /// This key is required to retrieve content when secure access is enabled.
        /// To retrieve content when secure access is enabled the <see cref="SecureAccessApiKey"/> must be set.
        /// </summary>
        public bool UseSecureAccess { get; set; }

        /// <summary>
        /// Gets or sets the API key that is used to retrieve content with the Production API when secure access is enabled.
        /// </summary>
        public string SecureAccessApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether a retry policy is used to make HTTP requests.
        /// </summary>
        public bool EnableRetryPolicy { get; set; } = true;

        /// <summary>
        /// Gets or sets configuration of the default retry policy.
        /// </summary>
        public DefaultRetryPolicyOptions DefaultRetryPolicyOptions { get; set; } = new DefaultRetryPolicyOptions();

        /// <summary>
        /// Gets or sets a value that determines if the client includes the total number of items matching the search criteria in response.
        /// This behavior can also be enabled for individual requests with the IncludeTotalCountParameter.
        /// </summary>
        public bool IncludeTotalCount { get; set; }

        /// <summary>
        /// The name of the service configuration this options object is related to.
        /// </summary>
        internal string Name { get; set; }
    }
}
