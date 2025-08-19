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
        public static DeliveryOptionsBuilder Create(string environmentId)
        {
            return new DeliveryOptionsBuilder(environmentId);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class with the specified environment ID.
        /// </summary>
        /// <param name="environmentId">The identifier of a Kontent.ai environment.</param>
        public static DeliveryOptionsBuilder Create(Guid environmentId)
        {
            return new DeliveryOptionsBuilder(environmentId.ToString());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class.
        /// </summary>
        [Obsolete("Use Create(environmentId) for a simpler API")]
        public static ILegacyDeliveryOptionsBuilder CreateInstance()
            => new DeliveryOptionsBuilderLegacy();

        private DeliveryOptionsBuilder(string environmentId)
        {
            _options = new DeliveryOptions { EnvironmentId = environmentId };
        }

        /// <summary>
        /// Configure for Production API.
        /// </summary>
        public DeliveryOptionsBuilder UseProduction()
        {
            _options = _options with { UsePreviewApi = false, UseSecureAccess = false };
            return this;
        }

        /// <summary>
        /// Configure for Production API with secure access.
        /// </summary>
        /// <param name="secureAccessApiKey">An API key for secure access.</param>
        public DeliveryOptionsBuilder UseProduction(string secureAccessApiKey)
        {
            _options = _options with { UsePreviewApi = false, UseSecureAccess = true, SecureAccessApiKey = secureAccessApiKey };
            return this;
        }

        /// <summary>
        /// Configure for Preview API.
        /// </summary>
        /// <param name="previewApiKey">A Preview API key.</param>
        public DeliveryOptionsBuilder UsePreview(string previewApiKey)
        {
            _options = _options with { UsePreviewApi = true, PreviewApiKey = previewApiKey, UseSecureAccess = false };
            return this;
        }

        /// <summary>
        /// Disable retry policy for HTTP requests.
        /// </summary>
        public DeliveryOptionsBuilder DisableRetryPolicy()
        {
            _options = _options with { EnableResilience = false };
            return this;
        }

        /// <summary>
        /// Use a custom endpoint for the Production or Preview API.
        /// </summary>
        /// <param name="endpoint">A custom endpoint URL.</param>
        public DeliveryOptionsBuilder WithCustomEndpoint(string endpoint)
        {
            SetCustomEndpoint(endpoint);
            return this;
        }

        /// <summary>
        /// Use a custom endpoint for the Production or Preview API.
        /// </summary>
        /// <param name="endpoint">A custom endpoint URI.</param>
        public DeliveryOptionsBuilder WithCustomEndpoint(Uri endpoint)
        {
            SetCustomEndpoint(endpoint.AbsoluteUri);
            return this;
        }

        /// <summary>
        /// Apply rendition of given preset to the asset URLs by default.
        /// </summary>
        /// <param name="presetCodename">Codename of the rendition preset to be applied automatically.</param>
        public DeliveryOptionsBuilder WithDefaultRenditionPreset(string presetCodename)
        {
            _options = _options with { DefaultRenditionPreset = presetCodename };
            return this;
        }

        /// <summary>
        /// Enable waiting for loading new content globally via DeliveryOptions.
        /// </summary>
        public DeliveryOptionsBuilder WithWaitForLoadingNewContent()
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

        #region Explicit interface implementations for IDeliveryOptionsBuilder

        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.UseProduction() => UseProduction();
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.UseProduction(string secureAccessApiKey) => UseProduction(secureAccessApiKey);
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.UsePreview(string previewApiKey) => UsePreview(previewApiKey);
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.DisableRetryPolicy() => DisableRetryPolicy();
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.WithCustomEndpoint(string endpoint) => WithCustomEndpoint(endpoint);
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.WithCustomEndpoint(Uri endpoint) => WithCustomEndpoint(endpoint);
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.WithDefaultRenditionPreset(string presetCodename) => WithDefaultRenditionPreset(presetCodename);
        IDeliveryOptionsBuilder IDeliveryOptionsBuilder.WithWaitForLoadingNewContent() => WithWaitForLoadingNewContent();

        #endregion
    }

    /// <summary>
    /// Legacy builder implementation for backward compatibility.
    /// </summary>
    [Obsolete]
    internal class DeliveryOptionsBuilderLegacy : IDeliveryApiConfiguration, ILegacyDeliveryOptionsBuilder, IOptionalDeliveryConfiguration
    {
        private string _environmentId = string.Empty;
        private bool _enableResilience = true;
        private string _productionEndpoint = "https://deliver.kontent.ai/";
        private string _previewEndpoint = "https://preview-deliver.kontent.ai/";
        private string? _previewApiKey;
        private bool _usePreviewApi;
        private bool _includeTotalCount;
        private bool _useSecureAccess;
        private string? _secureAccessApiKey;
        private DefaultRetryPolicyOptions _defaultRetryPolicyOptions = new();
        private string? _defaultRenditionPreset;
        private bool _waitForLoadingNewContent;

        [Obsolete]
        IDeliveryApiConfiguration ILegacyDeliveryOptionsBuilder.WithEnvironmentId(string environmentId)
        {
            _environmentId = environmentId;
            return this;
        }

        [Obsolete]
        IDeliveryApiConfiguration ILegacyDeliveryOptionsBuilder.WithEnvironmentId(Guid environmentId)
        {
            _environmentId = environmentId.ToString();
            return this;
        }


        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.IncludeTotalCount()
        {
            _includeTotalCount = true;
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.DisableRetryPolicy()
        {
            _enableResilience = false;
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithDefaultRetryPolicyOptions(DefaultRetryPolicyOptions retryPolicyOptions)
        {
            _defaultRetryPolicyOptions = retryPolicyOptions;
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UsePreviewApi(string previewApiKey)
        {
            _previewApiKey = previewApiKey;
            _usePreviewApi = true;
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseProductionApi()
            => this;

        [Obsolete]
        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseProductionApi(string secureAccessApiKey)
        {
            _secureAccessApiKey = secureAccessApiKey;
            _useSecureAccess = true;
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(string endpoint)
        {
            SetCustomEndpoint(endpoint);
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(Uri endpoint)
        {
            SetCustomEndpoint(endpoint.AbsoluteUri);
            return this;
        }

        [Obsolete]
        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithDefaultRenditionPreset(string presetCodename)
        {
            _defaultRenditionPreset = presetCodename;
            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WaitForLoadingNewContent()
        {
            _waitForLoadingNewContent = true;
            return this;
        }

        private void SetCustomEndpoint(string endpoint)
        {
            if (_usePreviewApi)
            {
                _previewEndpoint = endpoint;
            }
            else
            {
                _productionEndpoint = endpoint;
            }
        }

        DeliveryOptions IDeliveryOptionsBuild.Build()
        {
            var options = new DeliveryOptions
            {
                EnvironmentId = _environmentId,
                EnableResilience = _enableResilience,
                ProductionEndpoint = _productionEndpoint,
                PreviewEndpoint = _previewEndpoint,
                PreviewApiKey = _previewApiKey,
                UsePreviewApi = _usePreviewApi,
                IncludeTotalCount = _includeTotalCount,
                UseSecureAccess = _useSecureAccess,
                SecureAccessApiKey = _secureAccessApiKey,
                DefaultRenditionPreset = _defaultRenditionPreset,
                WaitForLoadingNewContent = _waitForLoadingNewContent
            };

            return options;
        }
    }
}
