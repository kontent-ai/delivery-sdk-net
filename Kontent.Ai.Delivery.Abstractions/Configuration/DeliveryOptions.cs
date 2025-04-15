using System;

namespace Kontent.Ai.Delivery.Abstractions
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
        /// Gets or sets the environment identifier.
        /// </summary>
        public string EnvironmentId { get; set; }

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
        /// Gets or sets a value of codename for the rendition preset to be applied by default to the base asset URL path.
        /// If no value is specified, asset URLs will always point to non-customized variant of the image.
        /// </summary>
        public string DefaultRenditionPreset { get; set; }

        /// <summary>
        /// The name of the service configuration this options object is related to.
        /// </summary>
        [Obsolete("#312")]
        internal string Name { get; set; }
        
        /// <summary>
        /// Replaces the base URL of the asset URLs in the API response. The asset URL will retain all the identifying information such as project id, asset id and file name.<br/>
        /// <b>NOTE: </b>Do not specify a trailing backslash in the URL.<br/>
        /// <b>Example where AssetUrlReplacement is defined as "https://www.example.com/assets":</b><br/>
        /// <b>Original Value:</b> https://preview-assets-us-01.kc-usercontent.com/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf <br/>
        /// <b>New Value:</b> https://www.example.com/assets/7ffda7b5-bfb2-4226-8b4f-d2e333638416/614cb0cc-9e62-4572-a408-fc8715f990e8/test-asset.pdf
        /// </summary>
        public string AssetUrlReplacement { get; set; }
    }
}
