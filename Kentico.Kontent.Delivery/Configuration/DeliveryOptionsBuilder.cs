using System;
using Kentico.Kontent.Delivery.Abstractions;
using Kentico.Kontent.Delivery.Abstractions.Configuration;
using DefaultRetryPolicyOptions = Kentico.Kontent.Delivery.Abstractions.Configuration.DefaultRetryPolicyOptions;

namespace Kentico.Kontent.Delivery.Configuration
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

        IDeliveryApiConfiguration IDeliveryOptionsBuilder.WithProjectId(string projectId)
        {
            projectId.ValidateProjectId();
            _deliveryOptions.ProjectId = projectId;

            return this;
        }

        IDeliveryApiConfiguration IDeliveryOptionsBuilder.WithProjectId(Guid projectId)
        {
            projectId.ValidateProjectId();
            _deliveryOptions.ProjectId = projectId.ToString();

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
