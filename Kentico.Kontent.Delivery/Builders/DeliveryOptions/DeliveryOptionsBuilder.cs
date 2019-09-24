using System;
using Kentico.Kontent.Delivery.Builders.DeliveryOptions;

namespace Kentico.Kontent.Delivery
{
    /// <summary>
    /// A builder class that can be used for creating a <see cref="DeliveryOptions"/> instance.
    /// </summary>
    public class DeliveryOptionsBuilder : IDeliveryApiConfiguration, IDeliveryOptionsBuilder, IOptionalDeliveryConfiguration
    {
        private readonly DeliveryOptions _deliveryOptions = new DeliveryOptions();

        /// <summary>
        /// Creates a new instance of the <see cref="DeliveryOptionsBuilder"/> class for building <see cref="DeliveryOptions"/>.
        /// </summary>
        public static IDeliveryOptionsBuilder CreateInstance()
            => new DeliveryOptionsBuilder();

        private DeliveryOptionsBuilder() {}

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

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WaitForLoadingNewContent
        {
            get
            {
                _deliveryOptions.WaitForLoadingNewContent = true;

                return this;
            }
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.DisableResilienceLogic
        {
            get
            {
                _deliveryOptions.EnableResilienceLogic = false;

                return this;
            }
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithMaxRetryAttempts(int attempts)
        {
            attempts.ValidateMaxRetryAttempts();
            _deliveryOptions.MaxRetryAttempts = attempts;
            _deliveryOptions.EnableResilienceLogic = attempts != 0;

            return this;
        }

        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UsePreviewApi(string previewApiKey)
        {
            previewApiKey.ValidateApiKey(nameof(previewApiKey));
            _deliveryOptions.PreviewApiKey = previewApiKey;
            _deliveryOptions.UsePreviewApi = true;

            return this;
        }
        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseProductionApi
            => this;

        IOptionalDeliveryConfiguration IDeliveryApiConfiguration.UseSecuredProductionApi(string securedProductionApiKey)
        {
            securedProductionApiKey.ValidateApiKey(nameof(securedProductionApiKey));
            _deliveryOptions.SecuredProductionApiKey = securedProductionApiKey;
            _deliveryOptions.UseSecuredProductionApi = true;

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(string endpoint)
        {
            endpoint.ValidateCustomEndpoint();
            if (_deliveryOptions.UsePreviewApi)
            {
                _deliveryOptions.PreviewEndpoint = endpoint;
            }
            else
            {
                _deliveryOptions.ProductionEndpoint = endpoint;
            }

            return this;
        }

        IOptionalDeliveryConfiguration IOptionalDeliveryConfiguration.WithCustomEndpoint(Uri endpoint)
        {
            endpoint.ValidateCustomEndpoint();
            if (_deliveryOptions.UsePreviewApi)
            {
                _deliveryOptions.PreviewEndpoint = endpoint.AbsoluteUri;
            }
            else
            {
                _deliveryOptions.ProductionEndpoint = endpoint.AbsoluteUri;
            }

            return this;
        }

        DeliveryOptions IDeliveryOptionsBuild.Build()
        {
            _deliveryOptions.Validate();

            return _deliveryOptions;
        }
    }
}
