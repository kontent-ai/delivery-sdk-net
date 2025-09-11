namespace Kontent.Ai.Delivery.Configuration
{
    /// <summary>
    /// A builder of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public class DeliveryOptionsBuilder : IDeliveryOptionsBuilder
    {
        private DeliveryOptions _options;

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class with the specified environment ID.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        public static IDeliveryOptionsBuilder Create(string environmentId)
        {
            return new DeliveryOptionsBuilder(environmentId);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class with the specified environment ID.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        public static IDeliveryOptionsBuilder Create(Guid environmentId)
        {
            return new DeliveryOptionsBuilder(environmentId.ToString());
        }

        private DeliveryOptionsBuilder(string environmentId)
        {
            _options = new DeliveryOptions { EnvironmentId = environmentId };
        }

        /// <summary>
        /// Configure for Production API.
        /// </summary>
        public IDeliveryOptionsBuilder UseProduction()
        {
            _options = _options with { UsePreviewApi = false, UseSecureAccess = false };
            return this;
        }

        /// <summary>
        /// Configure for Production API with secure access.
        /// </summary>
        /// <param name="secureAccessApiKey">An API key for secure access.</param>
        public IDeliveryOptionsBuilder UseProduction(string secureAccessApiKey)
        {
            _options = _options with { UsePreviewApi = false, UseSecureAccess = true, SecureAccessApiKey = secureAccessApiKey };
            return this;
        }

        /// <summary>
        /// Configure for Preview API.
        /// </summary>
        /// <param name="previewApiKey">A Preview API key.</param>
        public IDeliveryOptionsBuilder UsePreview(string previewApiKey)
        {
            _options = _options with { UsePreviewApi = true, PreviewApiKey = previewApiKey, UseSecureAccess = false };
            return this;
        }

        /// <summary>
        /// Disable retry policy for HTTP requests.
        /// </summary>
        public IDeliveryOptionsBuilder DisableRetryPolicy()
        {
            _options = _options with { EnableResilience = false };
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
            _options = _options with { DefaultRenditionPreset = presetCodename };
            return this;
        }

        /// <summary>
        /// Enable waiting for loading new content globally via DeliveryOptions.
        /// </summary>
        public IDeliveryOptionsBuilder WaitForLoadingNewContent()
        {
            _options = _options with { WaitForLoadingNewContent = true };
            return this;
        }

        private void SetCustomEndpoint(string endpoint)
        {
            if (_options.UsePreviewApi)
            {
                _options = _options with { PreviewEndpoint = endpoint };
            }
            else
            {
                _options = _options with { ProductionEndpoint = endpoint };
            }
        }

        /// <summary>
        /// Returns a new instance of the <see cref="DeliveryOptions"/> class.
        /// </summary>
        public DeliveryOptions Build() => _options;
    }
}
