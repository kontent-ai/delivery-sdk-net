using System;
using Kontent.Ai.Delivery.Abstractions;
using DefaultRetryPolicyOptions = Kontent.Ai.Delivery.Abstractions.DefaultRetryPolicyOptions;

namespace Kontent.Ai.Delivery.Configuration
{
    /// <summary>
    /// A builder of <see cref="DeliveryOptions"/> instances.
    /// </summary>
    public class DeliveryOptionsBuilder : IDeliveryApiConfiguration, IDeliveryOptionsBuilder, IOptionalDeliveryConfiguration
    {
        private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions();

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class.
        /// </summary>
        public static IDeliveryOptionsBuilder CreateInstance()
            => new DeliveryOptionsBuilder();

        private DeliveryOptionsBuilder() { }

        IDeliveryApiConfiguration IDeliveryOptionsBuilder.WithEnvironmentId(string environmentId)
        {
            environmentId.ValidateEnvironmentId();
            _deliveryOptions.EnvironmentId = environmentId;

            return this;
        }

        IDeliveryApiConfiguration IDeliveryOptionsBuilder.WithEnvironmentId(Guid environmentId)
        {
            environmentId.ValidateEnvironmentId();
            _deliveryOptions.EnvironmentId = environmentId.ToString();

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WaitForLoadingNewContent()
        {
            _deliveryOptions.WaitForLoadingNewContent = true;

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.IncludeTotalCount()
        {
            _deliveryOptions.IncludeTotalCount = true;

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.DisableRetryPolicy()
        {
            _deliveryOptions.EnableRetryPolicy = false;

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithDefaultRetryPolicyOptions(DefaultRetryPolicyOptions retryPolicyOptions)
        {
            retryPolicyOptions.ValidateRetryPolicyOptions();
            _deliveryOptions.DefaultRetryPolicyOptions = retryPolicyOptions;

            return this;
        }

        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UsePreviewApi(string previewApiKey)
        {
            previewApiKey.ValidateApiKey(nameof(previewApiKey));
            _deliveryOptions.PreviewApiKey = previewApiKey;
            _deliveryOptions.UsePreviewApi = true;

            return this;
        }
        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseProductionApi()
            => this;

        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseProductionApi(string secureAccessApiKey)
        {
            secureAccessApiKey.ValidateApiKey(nameof(secureAccessApiKey));
            _deliveryOptions.SecureAccessApiKey = secureAccessApiKey;
            _deliveryOptions.UseSecureAccess = true;

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(string endpoint)
        {
            endpoint.ValidateCustomEndpoint();
            SetCustomEndpoint(endpoint);
            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(Uri endpoint)
        {
            endpoint.ValidateCustomEndpoint();
            SetCustomEndpoint(endpoint.AbsoluteUri);
            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithDefaultRenditionPreset(string presetCodename)
        {
            _deliveryOptions.DefaultRenditionPreset = presetCodename;
            
            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithAssetUrlReplacement(string url)
        {
            _deliveryOptions.AssetUrlReplacement = url;
            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithAssetUrlReplacement(Uri url)
        {
            _deliveryOptions.AssetUrlReplacement = url.AbsoluteUri;
            return this;
        }

        private void SetCustomEndpoint(string endpoint)
        {
            if (_deliveryOptions.UsePreviewApi)
            {
                _deliveryOptions.PreviewEndpoint = endpoint;
            }
            else
            {
                _deliveryOptions.ProductionEndpoint = endpoint;
            }
        }

        DeliveryOptions IDeliveryOptionsBuild.Build()
        {
            _deliveryOptions.Validate();

            return _deliveryOptions;
        }
    }
}
